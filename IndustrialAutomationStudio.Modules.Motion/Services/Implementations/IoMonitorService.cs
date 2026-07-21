using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class IoMonitorService : IIoMonitorService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(100);

    private readonly IMotionCardService _cardService;
    private readonly IMotionCardConfigRepository _configRepository;
    private readonly IMotionLogService _logService;
    private readonly SemaphoreSlim _ioGate = new(1, 1);
    private readonly object _lifecycleSync = new();
    private CancellationTokenSource? _monitorCancellation;
    private Task? _monitorTask;
    private MotionCardConfig? _config;
    private int _leaseCount;
    private int _readFailureActive;
    private bool _disposed;

    public IoMonitorService(
        IMotionCardService cardService,
        IMotionCardConfigRepository configRepository,
        IMotionLogService logService)
    {
        _cardService = cardService;
        _configRepository = configRepository;
        _logService = logService;
    }

    public event EventHandler<IoSnapshot>? SnapshotChanged;
    public event EventHandler<IoMonitorError>? MonitorError;

    public bool IsMonitoring
    {
        get
        {
            lock (_lifecycleSync)
            {
                return _monitorTask is { IsCompleted: false };
            }
        }
    }

    public bool CanWriteDigitalOutputs => _cardService.CanWriteDigitalOutputs;

    public IDisposable AcquireMonitoring()
    {
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _leaseCount++;
            if (_monitorTask is null)
            {
                StartMonitorLocked();
            }
        }

        return new MonitorLease(this);
    }

    public async Task<IoSnapshot> ReadNowAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_cardService.IsConnected)
            {
                var disconnected = await CreateUnknownSnapshotAsync(cancellationToken)
                    .ConfigureAwait(false);
                PublishSnapshot(disconnected);
                return disconnected;
            }

            try
            {
                var snapshot = await _cardService.ReadIoSnapshotAsync(cancellationToken)
                    .ConfigureAwait(false);
                Interlocked.Exchange(ref _readFailureActive, 0);
                PublishSnapshot(snapshot);
                return snapshot;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var unknown = await CreateUnknownSnapshotAsync(cancellationToken)
                    .ConfigureAwait(false);
                HandleFailure("读取 IO", exception, logOnlyOnTransition: true);
                PublishSnapshot(unknown);
                return unknown;
            }
        }
        finally
        {
            _ioGate.Release();
        }
    }

    public async Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_cardService.CanWriteDigitalOutputs)
            {
                throw new InvalidOperationException("当前控制卡不允许写入数字输出。");
            }

            try
            {
                await _cardService.WriteDigitalOutputAsync(index, value, cancellationToken)
                    .ConfigureAwait(false);
                var snapshot = await _cardService.ReadIoSnapshotAsync(cancellationToken)
                    .ConfigureAwait(false);
                Interlocked.Exchange(ref _readFailureActive, 0);
                PublishSnapshot(snapshot);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await PublishReadbackAfterWriteFailureAsync(exception, cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _ioGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task? monitorTask;
        lock (_lifecycleSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _leaseCount = 0;
            _monitorCancellation?.Cancel();
            monitorTask = _monitorTask;
        }

        if (monitorTask is not null)
        {
            await monitorTask.ConfigureAwait(false);
        }

        _ioGate.Dispose();
    }

    private void StartMonitorLocked()
    {
        var cancellation = new CancellationTokenSource();
        _monitorCancellation = cancellation;
        _monitorTask = MonitorAsync(cancellation);
    }

    private async Task MonitorAsync(CancellationTokenSource owner)
    {
        try
        {
            _ = await ReadNowAsync(owner.Token).ConfigureAwait(false);
            using var timer = new PeriodicTimer(PollingInterval);
            while (await timer.WaitForNextTickAsync(owner.Token).ConfigureAwait(false))
            {
                _ = await ReadNowAsync(owner.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (owner.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_lifecycleSync)
            {
                if (ReferenceEquals(_monitorCancellation, owner))
                {
                    _monitorCancellation = null;
                    _monitorTask = null;
                    owner.Dispose();
                    if (_leaseCount > 0 && !_disposed)
                    {
                        StartMonitorLocked();
                    }
                }
            }
        }
    }

    private async Task<IoSnapshot> CreateUnknownSnapshotAsync(
        CancellationToken cancellationToken)
    {
        _config ??= await _configRepository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return new IoSnapshot(
            new bool?[_config.DiCount],
            new bool?[_config.DoCount]);
    }

    private async Task PublishReadbackAfterWriteFailureAsync(
        Exception writeException,
        CancellationToken cancellationToken)
    {
        HandleFailure("写入 DO", writeException, logOnlyOnTransition: false);
        try
        {
            var snapshot = _cardService.IsConnected
                ? await _cardService.ReadIoSnapshotAsync(cancellationToken).ConfigureAwait(false)
                : await CreateUnknownSnapshotAsync(cancellationToken).ConfigureAwait(false);
            PublishSnapshot(snapshot);
        }
        catch (Exception readException) when (readException is not OperationCanceledException)
        {
            var unknown = await CreateUnknownSnapshotAsync(cancellationToken).ConfigureAwait(false);
            HandleFailure("写入后读回 IO", readException, logOnlyOnTransition: false);
            PublishSnapshot(unknown);
        }
    }

    private void HandleFailure(
        string operation,
        Exception exception,
        bool logOnlyOnTransition)
    {
        var shouldLog = !logOnlyOnTransition ||
                        Interlocked.Exchange(ref _readFailureActive, 1) == 0;
        if (shouldLog)
        {
            _logService.Log(new MotionLogEntry(
                DateTimeOffset.Now,
                MotionLogLevel.Error,
                "IoMonitor",
                "IO",
                operation,
                exception.Message,
                exception.ToString()));
        }

        MonitorError?.Invoke(this, new IoMonitorError(
            operation,
            exception.Message,
            exception));
    }

    private void PublishSnapshot(IoSnapshot snapshot) =>
        SnapshotChanged?.Invoke(this, snapshot);

    private void ReleaseMonitoring()
    {
        lock (_lifecycleSync)
        {
            if (_leaseCount == 0)
            {
                return;
            }

            _leaseCount--;
            if (_leaseCount == 0)
            {
                _monitorCancellation?.Cancel();
            }
        }
    }

    private sealed class MonitorLease(IoMonitorService owner) : IDisposable
    {
        private IoMonitorService? _owner = owner;

        public void Dispose() =>
            Interlocked.Exchange(ref _owner, null)?.ReleaseMonitoring();
    }
}
