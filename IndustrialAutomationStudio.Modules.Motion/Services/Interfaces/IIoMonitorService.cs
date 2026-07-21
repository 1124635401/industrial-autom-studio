using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public sealed record IoMonitorError(
    string Operation,
    string Message,
    Exception Exception);

public interface IIoMonitorService : IAsyncDisposable
{
    event EventHandler<IoSnapshot>? SnapshotChanged;
    event EventHandler<IoMonitorError>? MonitorError;
    bool IsMonitoring { get; }
    bool CanWriteDigitalOutputs { get; }
    IDisposable AcquireMonitoring();
    Task<IoSnapshot> ReadNowAsync(CancellationToken cancellationToken = default);
    Task WriteDigitalOutputAsync(
        int index,
        bool value,
        CancellationToken cancellationToken = default);
}
