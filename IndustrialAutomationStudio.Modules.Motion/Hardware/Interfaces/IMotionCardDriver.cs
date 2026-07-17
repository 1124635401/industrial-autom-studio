using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;

public interface IMotionCardDriver : IAsyncDisposable
{
    int CardNo { get; }
    string DriverKey { get; }
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
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
