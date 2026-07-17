using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.Mock;

public sealed class MockMotionCardDriver : IMotionCardDriver
{
    private static readonly string[] AxisNames =
        ["WorkPosition", "Z1", "Z2", "R", "Y1", "X1", "Y2", "X2"];

    private readonly MotionCardConfig _config;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<AxisAddress, AxisConfig> _axes;
    private bool _disposed;

    public MockMotionCardDriver(MotionCardConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _axes = AxisNames
            .Select((name, axisNo) => AxisConfig.CreateDefault(
                new AxisAddress(config.CardNo, axisNo),
                name))
            .ToDictionary(axis => axis.Address);
    }

    public int CardNo => _config.CardNo;
    public string DriverKey => "Mock";
    public bool IsConnected { get; private set; }

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

    private MotionDriverException Failure(string operation, string message) =>
        new(message, DriverKey, operation, CardNo);
}
