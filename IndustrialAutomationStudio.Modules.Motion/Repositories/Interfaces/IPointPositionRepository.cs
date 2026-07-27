using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

public interface IPointPositionRepository
{
    Task<IReadOnlyList<PositionPoint>> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<PositionPoint> points,
        CancellationToken cancellationToken = default);
}
