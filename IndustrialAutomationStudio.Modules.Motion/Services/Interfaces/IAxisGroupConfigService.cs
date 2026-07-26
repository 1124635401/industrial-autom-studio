using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IAxisGroupConfigService
{
    Task<IReadOnlyList<AxisGroupConfig>> LoadAsync(
        CancellationToken cancellationToken = default);

    string? ValidateName(
        string name,
        IEnumerable<AxisGroupConfig> groups,
        string? currentGroupId = null);

    Task SaveAsync(
        IReadOnlyCollection<AxisGroupConfig> groups,
        CancellationToken cancellationToken = default);
}
