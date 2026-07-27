using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionCardService : IAsyncDisposable
{
    bool IsConnected { get; }
    bool CanWriteDigitalOutputs { get; }
    bool CanControlMotion => false;
    IReadOnlyList<string> AvailableDriverKeys { get; }
    event EventHandler<bool>? ConnectionChanged;
    Task ConnectAsync(
        MotionCardConfig? config = null,
        CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<MotionCardInfo> GetCardInfoAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AxisConfig>> ScanAxesAsync(CancellationToken cancellationToken = default);
    Task<AxisConfig> ReadAxisConfigAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default);
    Task WriteAxisConfigAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default);
    Task<IoSnapshot> ReadIoSnapshotAsync(CancellationToken cancellationToken = default);
    Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AxisPulseState>> ReadAxisStatesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<AxisPulseState>>(
            new NotSupportedException("当前服务不支持运动控制。"));
    Task StartJogAsync(
        AxisAddress address,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("当前服务不支持运动控制。"));
    Task MoveAbsoluteAsync(
        AxisPulseTarget target,
        double velocityPulsesPerSecond,
        MotionProfile profile,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("当前服务不支持运动控制。"));
    Task MoveSynchronizedAsync(
        IReadOnlyList<AxisPulseTarget> targets,
        double accelerationPulsesPerSecondSquared,
        double velocityPulsesPerSecond,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("当前服务不支持运动控制。"));
    Task StopAxesAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        MotionStopMode mode,
        CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("当前服务不支持运动控制。"));
}
