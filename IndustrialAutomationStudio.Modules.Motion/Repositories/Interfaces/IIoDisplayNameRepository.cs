namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

public interface IIoDisplayNameRepository
{
    Task<IReadOnlyDictionary<string, string>> LoadAsync(
        CancellationToken cancellationToken = default);
    Task SaveAsync(
        IReadOnlyDictionary<string, string> displayNames,
        CancellationToken cancellationToken = default);
}
