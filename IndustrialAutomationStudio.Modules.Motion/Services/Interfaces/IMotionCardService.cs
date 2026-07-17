using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionCardService : IAsyncDisposable
{
    bool IsConnected { get; }
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
}
