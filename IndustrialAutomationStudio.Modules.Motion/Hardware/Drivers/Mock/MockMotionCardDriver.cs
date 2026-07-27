using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.Mock;

public sealed class MockMotionCardDriver : IMotionCardDriver
{
    private readonly MotionCardConfig _config;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<AxisAddress, AxisConfig> _axes;
    private readonly Dictionary<AxisAddress, AxisPulseState> _axisStates;
    private readonly bool?[] _digitalInputs;
    private readonly bool?[] _digitalOutputs;
    private bool _disposed;

    public MockMotionCardDriver(MotionCardConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _axes = Enumerable.Range(0, Math.Max(0, config.AxisCount))
            .Select(axisNo => AxisConfig.CreateDefault(
                new AxisAddress(config.CardNo, axisNo),
                $"Axis{axisNo}"))
            .ToDictionary(axis => axis.Address);
        _axisStates = _axes.Keys.ToDictionary(
            address => address,
            address => new AxisPulseState
            {
                Address = address,
                ServoOn = true,
                InPosition = true
            });
        _digitalInputs = Enumerable.Range(1, Math.Max(0, config.DiCount))
            .Select(index => (bool?)(index % 2 == 1))
            .ToArray();
        _digitalOutputs = new bool?[Math.Max(0, config.DoCount)];
        Array.Fill(_digitalOutputs, false);
    }

    public int CardNo => _config.CardNo;
    public string DriverKey => "Mock";
    public bool IsConnected { get; private set; }
    public bool CanWriteDigitalOutputs => true;
    public bool CanControlMotion => true;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            IsConnected = true;
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
            if (!_disposed)
            {
                IsConnected = false;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MotionCardInfo> GetCardInfoAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("GetCardInfo");
            return new MotionCardInfo
            {
                CardNo = CardNo,
                CardName = _config.CardName,
                CardType = _config.CardType,
                DriverKey = DriverKey,
                FirmwareVersion = "MOCK-1.0.0",
                AxisCount = _axes.Count,
                DiCount = _config.DiCount,
                DoCount = _config.DoCount
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AxisConfig>> ScanAxesAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ScanAxes");
            return _axes.Values
                .OrderBy(axis => axis.Address.AxisNo)
                .Select(axis => axis with { })
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AxisConfig> ReadAxisConfigAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ReadAxisConfig");
            if (_axes.TryGetValue(address, out var axis))
            {
                return axis with { };
            }

            throw Failure("ReadAxisConfig", $"未找到轴 {address.CardNo}:{address.AxisNo}。");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAxisConfigAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("WriteAxisConfig");
            if (config.Address.CardNo != CardNo)
            {
                throw Failure("WriteAxisConfig", $"轴不属于控制卡 {CardNo}。");
            }

            _axes[config.Address] = config with { };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IoSnapshot> ReadIoSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ReadIoSnapshot");
            return new IoSnapshot(
                _digitalInputs.ToArray(),
                _digitalOutputs.ToArray());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("WriteDigitalOutput");
            if (index < 1 || index > _digitalOutputs.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _digitalOutputs[index - 1] = value;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AxisPulseState>> ReadAxisStatesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("ReadAxisStates");
            return addresses
                .Select(address => GetAxisState(address, "ReadAxisStates") with { })
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StartJogAsync(
        AxisAddress address,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("StartJog");
            if (!double.IsFinite(velocityPulsesPerSecond) ||
                velocityPulsesPerSecond == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(velocityPulsesPerSecond));
            }

            var state = GetAxisState(address, "StartJog");
            _axisStates[address] = state with
            {
                VelocityPulsesPerSecond = velocityPulsesPerSecond,
                IsMoving = true,
                InPosition = false
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MoveAbsoluteAsync(
        AxisPulseTarget target,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("MoveAbsolute");
            CompleteMove(target, "MoveAbsolute");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MoveSynchronizedAsync(
        IReadOnlyList<AxisPulseTarget> targets,
        double accelerationPulsesPerSecondSquared,
        double velocityPulsesPerSecond,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targets);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("MoveSynchronized");
            foreach (var target in targets)
            {
                _ = GetAxisState(target.Address, "MoveSynchronized");
            }

            foreach (var target in targets)
            {
                CompleteMove(target, "MoveSynchronized");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAxesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        MotionStopMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected("StopAxes");
            foreach (var address in addresses)
            {
                var state = GetAxisState(address, "StopAxes");
                _axisStates[address] = state with
                {
                    VelocityPulsesPerSecond = 0,
                    IsMoving = false,
                    InPosition = true
                };
            }
        }
        finally
        {
            _gate.Release();
        }
    }

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

    private void EnsureConnected(string operation)
    {
        ThrowIfDisposed();
        if (!IsConnected)
        {
            throw Failure(operation, $"控制卡 {CardNo} 尚未连接。");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private AxisPulseState GetAxisState(AxisAddress address, string operation) =>
        _axisStates.TryGetValue(address, out var state)
            ? state
            : throw Failure(operation, $"未找到轴 {address.CardNo}:{address.AxisNo}。");

    private void CompleteMove(AxisPulseTarget target, string operation)
    {
        var state = GetAxisState(target.Address, operation);
        _axisStates[target.Address] = state with
        {
            CommandPulses = target.TargetPulses,
            ActualPulses = target.TargetPulses,
            VelocityPulsesPerSecond = 0,
            IsMoving = false,
            InPosition = true
        };
    }

    private MotionDriverException Failure(string operation, string message) =>
        new(message, DriverKey, operation, CardNo);
}
