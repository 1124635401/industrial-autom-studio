using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Hardware;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class MotionCardService : IMotionCardService
{
    private readonly DriverRegistry _registry;
    private readonly IMotionCardConfigRepository _configRepository;
    private readonly IMotionLogService _logService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IMotionCardDriver? _driver;
    private bool _disposed;

    public MotionCardService(
        DriverRegistry registry,
        IMotionCardConfigRepository configRepository,
        IMotionLogService logService)
    {
        _registry = registry;
        _configRepository = configRepository;
        _logService = logService;
    }

    public bool IsConnected => _driver?.IsConnected == true;
    public bool CanWriteDigitalOutputs =>
        _driver is { IsConnected: true, CanWriteDigitalOutputs: true };
    public IReadOnlyList<string> AvailableDriverKeys => _registry.DriverKeys;
    public event EventHandler<bool>? ConnectionChanged;

    public async Task ConnectAsync(
        MotionCardConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            config ??= await _configRepository.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (_driver is not null)
            {
                await _driver.DisposeAsync().ConfigureAwait(false);
            }

            _driver = _registry.Resolve(config.DriverKey).Create(config);
            await _driver.ConnectAsync(cancellationToken).ConfigureAwait(false);
            ConnectionChanged?.Invoke(this, true);
            Log(MotionLogLevel.Information, "连接", $"控制卡 {config.CardNo} 已连接");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log(MotionLogLevel.Error, "连接", "连接失败", exception.Message);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_driver is not null)
            {
                await _driver.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                await _driver.DisposeAsync().ConfigureAwait(false);
                _driver = null;
                ConnectionChanged?.Invoke(this, false);
                Log(MotionLogLevel.Information, "断开", "控制卡已断开");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<MotionCardInfo> GetCardInfoAsync(CancellationToken cancellationToken = default) =>
        RequireDriver().GetCardInfoAsync(cancellationToken);

    public Task<IReadOnlyList<AxisConfig>> ScanAxesAsync(
        CancellationToken cancellationToken = default) =>
        RequireDriver().ScanAxesAsync(cancellationToken);

    public Task<AxisConfig> ReadAxisConfigAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default) =>
        RequireDriver().ReadAxisConfigAsync(address, cancellationToken);

    public Task WriteAxisConfigAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default) =>
        RequireDriver().WriteAxisConfigAsync(config, cancellationToken);

    public Task<IoSnapshot> ReadIoSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        RequireDriver().ReadIoSnapshotAsync(cancellationToken);

    public Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default) =>
        RequireDriver().WriteDigitalOutputAsync(index, value, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        _disposed = true;
        _gate.Dispose();
    }

    private IMotionCardDriver RequireDriver()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _driver is { IsConnected: true }
            ? _driver
            : throw new InvalidOperationException("运动控制卡尚未连接。");
    }

    private void Log(
        MotionLogLevel level,
        string operation,
        string result,
        string? exception = null) => _logService.Log(new MotionLogEntry(
        DateTimeOffset.Now,
        level,
        "MotionCard",
        _driver is null ? "Card" : $"Card{_driver.CardNo}",
        operation,
        result,
        exception));
}
