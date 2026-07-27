using System.IO;
using System.Text.Json;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

public sealed class JsonPointPositionRepository : IPointPositionRepository
{
    private readonly string _path;
    private readonly JsonFileStore _store = new();

    public JsonPointPositionRepository(MotionModuleOptions options)
        : this(options.ConfigDirectory)
    {
    }

    public JsonPointPositionRepository(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _path = Path.Combine(configDirectory, "PointConfig.json");
    }

    public async Task<IReadOnlyList<PositionPoint>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<PositionPoint>();
        }

        try
        {
            var points = await _store.ReadAsync<List<PositionPoint>>(
                    _path,
                    cancellationToken)
                .ConfigureAwait(false);
            return Normalize(points);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            var backupPath = BackupInvalidFile();
            throw new InvalidDataException(
                $"点位配置文件损坏，原文件已备份到 '{backupPath}'。",
                exception);
        }
    }

    public async Task SaveAsync(
        IReadOnlyCollection<PositionPoint> points,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(points);
        var normalized = Normalize(points);
        await _store.WriteAtomicAsync(_path, normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    private string BackupInvalidFile()
    {
        var directory = Path.GetDirectoryName(_path)!;
        var backupPath = Path.Combine(
            directory,
            $"PointConfig.invalid.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json");
        File.Move(_path, backupPath);
        return backupPath;
    }

    private static PositionPoint[] Normalize(IEnumerable<PositionPoint> points)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var namesByGroup =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var normalized = new List<PositionPoint>();

        foreach (var point in points)
        {
            if (point is null)
            {
                throw new InvalidDataException("点位配置中包含空点位。");
            }

            var id = RequireText(point.Id, "点位 ID");
            if (!ids.Add(id))
            {
                throw new InvalidDataException($"点位 ID '{id}' 重复。");
            }

            var groupId = RequireText(point.GroupId, "分组 ID");
            var name = RequireText(point.Name, "点位名称");
            var groupNames = namesByGroup.GetValueOrDefault(groupId);
            if (groupNames is null)
            {
                groupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                namesByGroup.Add(groupId, groupNames);
            }

            if (!groupNames.Add(name))
            {
                throw new InvalidDataException(
                    $"分组 '{groupId}' 的点位名称 '{name}' 重复。");
            }

            if (!double.IsFinite(point.Speed) || point.Speed <= 0)
            {
                throw new InvalidDataException($"点位 '{name}' 的速度必须是大于 0 的有限数值。");
            }

            if (point.AxisPositions is null || point.AxisPositions.Count == 0)
            {
                throw new InvalidDataException($"点位 '{name}' 必须包含至少一个轴位置。");
            }

            var addresses = new HashSet<AxisAddress>();
            var axisPositions = new List<PointAxisPosition>(point.AxisPositions.Count);
            foreach (var axisPosition in point.AxisPositions)
            {
                if (axisPosition is null)
                {
                    throw new InvalidDataException($"点位 '{name}' 包含空轴位置。");
                }

                if (!addresses.Add(axisPosition.Address))
                {
                    throw new InvalidDataException(
                        $"点位 '{name}' 的轴地址 {Format(axisPosition.Address)} 重复。");
                }

                if (!double.IsFinite(axisPosition.Position))
                {
                    throw new InvalidDataException(
                        $"点位 '{name}' 的轴 {Format(axisPosition.Address)} 位置必须是有限数值。");
                }

                axisPositions.Add(axisPosition with { });
            }

            normalized.Add(point with
            {
                Id = id,
                GroupId = groupId,
                Name = name,
                AxisPositions = axisPositions
                    .OrderBy(position => position.Address.CardNo)
                    .ThenBy(position => position.Address.AxisNo)
                    .ToList()
            });
        }

        return normalized
            .OrderBy(point => point.GroupId, StringComparer.Ordinal)
            .ThenBy(point => point.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(point => point.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RequireText(string? value, string label)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException($"{label}不能为空。");
        }

        return normalized;
    }

    private static string Format(AxisAddress address) =>
        $"{address.CardNo}/{address.AxisNo}";
}
