using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionConfigurationService
{
    Task<IReadOnlyList<AxisConfig>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(
        IReadOnlyCollection<AxisConfig> axes,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AxisConfig>> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
    Task ExportAsync(
        IReadOnlyCollection<AxisConfig> axes,
        string filePath,
        CancellationToken cancellationToken = default);
}
