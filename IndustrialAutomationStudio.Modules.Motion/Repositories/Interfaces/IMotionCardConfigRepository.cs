using System.Threading;
using System.Threading.Tasks;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

public interface IMotionCardConfigRepository
{
    Task<MotionCardConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(MotionCardConfig config, CancellationToken cancellationToken = default);
}
