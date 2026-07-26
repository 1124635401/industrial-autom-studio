using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

public interface IAxisGroupConfigRepository
{
    Task<IReadOnlyList<AxisGroupConfig>> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<AxisGroupConfig> groups,
        CancellationToken cancellationToken = default);
}
