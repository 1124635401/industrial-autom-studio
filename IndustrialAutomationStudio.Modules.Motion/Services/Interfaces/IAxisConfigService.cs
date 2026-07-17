using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IAxisConfigService
{
    Task<IReadOnlyList<AxisConfig>> LoadAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AxisConfig>> ScanAndMergeAsync(CancellationToken cancellationToken = default);
    AxisConfigValidationResult Validate(AxisConfig config);
    Task SaveAsync(AxisConfig config, CancellationToken cancellationToken = default);
    Task SaveAllAsync(
        IReadOnlyCollection<AxisConfig> configs,
        CancellationToken cancellationToken = default);
    Task<AxisConfig> ReadFromCardAsync(
        AxisAddress address,
        CancellationToken cancellationToken = default);
    Task<AxisConfig> WriteToCardAsync(
        AxisConfig config,
        CancellationToken cancellationToken = default);
}
