using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Json;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class MotionConfigurationService : IMotionConfigurationService
{
    private readonly IAxisConfigRepository _repository;
    private readonly JsonFileStore _fileStore = new();

    public MotionConfigurationService(IAxisConfigRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AxisConfig>> LoadAsync(
        CancellationToken cancellationToken = default) =>
        _repository.LoadAsync(cancellationToken);

    public Task SaveAsync(
        IReadOnlyCollection<AxisConfig> axes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(axes);
        return _repository.SaveAsync(axes, cancellationToken);
    }

    public async Task<IReadOnlyList<AxisConfig>> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return await _fileStore.ReadAsync<List<AxisConfig>>(filePath, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task ExportAsync(
        IReadOnlyCollection<AxisConfig> axes,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return _fileStore.WriteAtomicAsync(filePath, axes, cancellationToken);
    }
}
