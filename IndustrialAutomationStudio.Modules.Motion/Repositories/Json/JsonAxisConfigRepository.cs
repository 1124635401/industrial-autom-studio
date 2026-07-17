using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

public sealed class JsonAxisConfigRepository : IAxisConfigRepository
{
    private const string ResourceName = "IndustrialAutomationStudio.Modules.Motion.Defaults.AxisConfig.json";
    private readonly string _path;
    private readonly JsonFileStore _store = new();

    public JsonAxisConfigRepository(MotionModuleOptions options)
        : this(options.ConfigDirectory)
    {
    }

    public JsonAxisConfigRepository(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _path = Path.Combine(configDirectory, "AxisConfig.json");
    }

    public async Task<IReadOnlyList<AxisConfig>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var values = await _store.ReadAsync<List<AxisConfig>>(_path, cancellationToken)
                .ConfigureAwait(false);
            EnsureUnique(values);
            return values;
        }
        catch (JsonException)
        {
            BackupInvalidFile();
            return await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(
        IReadOnlyCollection<AxisConfig> configs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configs);
        EnsureUnique(configs);
        var ordered = configs.OrderBy(axis => axis.Address.CardNo)
            .ThenBy(axis => axis.Address.AxisNo)
            .ToArray();
        await _store.WriteAtomicAsync(_path, ordered, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<AxisConfig>> RestoreDefaultsAsync(
        CancellationToken cancellationToken)
    {
        var defaults = await _store.ReadEmbeddedAsync<List<AxisConfig>>(
                ResourceName,
                cancellationToken)
            .ConfigureAwait(false);
        await SaveAsync(defaults, cancellationToken).ConfigureAwait(false);
        return defaults;
    }

    private void BackupInvalidFile()
    {
        var directory = Path.GetDirectoryName(_path)!;
        var backup = Path.Combine(
            directory,
            $"AxisConfig.invalid.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json");
        File.Move(_path, backup);
    }

    private static void EnsureUnique(IEnumerable<AxisConfig> configs)
    {
        var addresses = new HashSet<AxisAddress>();
        foreach (var config in configs)
        {
            if (!addresses.Add(config.Address))
            {
                throw new InvalidDataException(
                    $"轴地址 {config.Address.CardNo}:{config.Address.AxisNo} 重复。");
            }
        }
    }
}
