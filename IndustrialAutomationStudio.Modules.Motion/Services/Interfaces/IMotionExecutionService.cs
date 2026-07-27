using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionExecutionService
{
    bool IsMotionAvailable { get; }

    Task<IReadOnlyList<AxisState>> ReadAxisStatesAsync(
        IReadOnlyCollection<AxisConfig> axes,
        CancellationToken cancellationToken = default);

    Task StartJogAsync(
        AxisConfig axis,
        int direction,
        double speed,
        CancellationToken cancellationToken = default);

    Task MoveToPointAsync(
        AxisGroupConfig group,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        PositionPoint point,
        CancellationToken cancellationToken = default);

    Task StopAsync(
        IReadOnlyCollection<AxisAddress> addresses,
        MotionStopMode mode = MotionStopMode.Smooth,
        CancellationToken cancellationToken = default);
}
