using System.IO;
using System.Text.Json;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

public sealed class JsonIoDisplayNameRepository : IIoDisplayNameRepository
{
    private readonly string _path;
    private readonly IMotionLogService _logService;
    private readonly JsonFileStore _store = new();

    public JsonIoDisplayNameRepository(
        MotionModuleOptions options,
        IMotionLogService logService)
        : this(options.ConfigDirectory, logService)
    {
    }

    public JsonIoDisplayNameRepository(
        string configDirectory,
        IMotionLogService logService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _path = Path.Combine(configDirectory, "IoDisplayNames.json");
        _logService = logService;
    }

    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return Empty();
        }

        try
        {
            var values = await _store.ReadAsync<Dictionary<string, string>>(
                    _path,
                    cancellationToken)
                .ConfigureAwait(false);
            return Normalize(values);
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or UnauthorizedAccessException)
        {
            _logService.Log(new MotionLogEntry(
                DateTimeOffset.Now,
                MotionLogLevel.Error,
                "IoMonitor",
                "IoDisplayNames.json",
                "加载显示名称",
                exception.Message,
                exception.ToString()));
            return Empty();
        }
    }

    public Task SaveAsync(
        IReadOnlyDictionary<string, string> displayNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(displayNames);
        return _store.WriteAtomicAsync(
            _path,
            Normalize(displayNames),
            cancellationToken);
    }

    private static Dictionary<string, string> Normalize(
        IEnumerable<KeyValuePair<string, string>> values) =>
        values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) &&
                           !string.IsNullOrWhiteSpace(pair.Value))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Trim(),
                StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, string> Empty() =>
        new Dictionary<string, string>(StringComparer.Ordinal);
}
