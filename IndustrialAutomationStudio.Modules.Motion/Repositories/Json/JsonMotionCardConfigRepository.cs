using System.IO;
using System.Text.Json;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

public sealed class JsonMotionCardConfigRepository : IMotionCardConfigRepository
{
    private const string ResourceName = "IndustrialAutomationStudio.Modules.Motion.Defaults.MotionCardConfig.json";
    private readonly string _path;
    private readonly JsonFileStore _store = new();

    public JsonMotionCardConfigRepository(MotionModuleOptions options)
        : this(options.ConfigDirectory)
    {
    }

    public JsonMotionCardConfigRepository(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _path = Path.Combine(configDirectory, "MotionCardConfig.json");
    }

    public async Task<MotionCardConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await _store.ReadAsync<MotionCardConfig>(_path, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            var directory = Path.GetDirectoryName(_path)!;
            File.Move(
                _path,
                Path.Combine(directory, $"MotionCardConfig.invalid.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json"));
            return await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SaveAsync(
        MotionCardConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return _store.WriteAtomicAsync(_path, config, cancellationToken);
    }

    private async Task<MotionCardConfig> RestoreDefaultsAsync(CancellationToken cancellationToken)
    {
        var config = await _store.ReadEmbeddedAsync<MotionCardConfig>(
                ResourceName,
                cancellationToken)
            .ConfigureAwait(false);
        await SaveAsync(config, cancellationToken).ConfigureAwait(false);
        return config;
    }
}
