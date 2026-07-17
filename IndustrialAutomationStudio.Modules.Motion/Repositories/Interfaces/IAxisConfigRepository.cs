using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

public interface IAxisConfigRepository
{
    Task<IReadOnlyList<AxisConfig>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(
        IReadOnlyCollection<AxisConfig> configs,
        CancellationToken cancellationToken = default);
}
