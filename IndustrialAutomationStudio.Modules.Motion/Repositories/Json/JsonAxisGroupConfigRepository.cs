using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

public sealed class JsonAxisGroupConfigRepository : IAxisGroupConfigRepository
{
    private readonly string _path;
    private readonly JsonFileStore _store = new();

    public JsonAxisGroupConfigRepository(MotionModuleOptions options)
        : this(options.ConfigDirectory)
    {
    }

    public JsonAxisGroupConfigRepository(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _path = Path.Combine(configDirectory, "AxisGroupConfig.json");
    }

    public async Task<IReadOnlyList<AxisGroupConfig>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<AxisGroupConfig>();
        }

        try
        {
            var documents = await _store.ReadAsync<List<AxisGroupConfigDocument>>(
                    _path,
                    cancellationToken)
                .ConfigureAwait(false);
            return Normalize(documents.Select(MapFromDocument));
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            var backupPath = BackupInvalidFile();
            throw new InvalidDataException(
                $"分组配置文件损坏，原文件已备份到 '{backupPath}'。",
                exception);
        }
    }

    public async Task SaveAsync(
        IReadOnlyCollection<AxisGroupConfig> groups,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var normalized = Normalize(groups);
        var documents = normalized.Select(MapToDocument).ToArray();
        await _store.WriteAtomicAsync(_path, documents, cancellationToken)
            .ConfigureAwait(false);
    }

    private string BackupInvalidFile()
    {
        var directory = Path.GetDirectoryName(_path)!;
        var backupPath = Path.Combine(
            directory,
            $"AxisGroupConfig.invalid.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json");
        File.Move(_path, backupPath);
        return backupPath;
    }

    private static AxisGroupConfig[] Normalize(IEnumerable<AxisGroupConfig> groups)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<AxisGroupConfig>();

        foreach (var group in groups)
        {
            if (group is null)
            {
                throw new InvalidDataException("分组配置中包含空分组。");
            }

            if (string.IsNullOrWhiteSpace(group.Id) || !ids.Add(group.Id))
            {
                throw new InvalidDataException($"分组 ID '{group.Id}' 为空或重复。");
            }

            if (group.Name is null)
            {
                throw new InvalidDataException($"分组 '{group.Id}' 的名称缺失。");
            }

            var name = group.Name.Trim();
            if (name.Length == 0)
            {
                throw new InvalidDataException("分组名称不能为空。");
            }

            if (name.Length > 50)
            {
                throw new InvalidDataException("分组名称不能超过 50 个字符。");
            }

            if (!names.Add(name))
            {
                throw new InvalidDataException($"分组名称 '{name}' 重复。");
            }

            if (group.Members is null)
            {
                throw new InvalidDataException($"分组 '{group.Id}' 的轴成员集合缺失。");
            }

            var addresses = new Dictionary<AxisAddress, AxisGroupMember>();
            var roles = new Dictionary<AxisRole, AxisGroupMember>();
            foreach (var member in group.Members)
            {
                if (member is null)
                {
                    throw new InvalidDataException($"分组 '{group.Id}' 中包含空轴成员。");
                }

                if (!addresses.TryAdd(member.Address, member))
                {
                    throw new InvalidDataException(
                        $"分组 '{name}' 的轴地址 {Format(member.Address)} 重复。");
                }

                if (!Enum.IsDefined(member.Role))
                {
                    throw new InvalidDataException(
                        $"分组 '{name}' 包含未知轴角色 '{(int)member.Role}'。");
                }

                if (member.Role != AxisRole.None
                    && !roles.TryAdd(member.Role, member))
                {
                    var existing = roles[member.Role];
                    throw new InvalidDataException(
                        $"分组 '{name}' 的角色 {member.Role} 同时分配给 "
                        + $"{Format(existing.Address)} 和 {Format(member.Address)}。");
                }
            }

            var members = group.Members
                .OrderBy(member => member.Address.CardNo)
                .ThenBy(member => member.Address.AxisNo)
                .Select(CloneMember)
                .ToList();
            normalized.Add(group with
            {
                Name = name,
                Members = members
            });
        }

        return normalized.ToArray();
    }

    private static AxisGroupConfig MapFromDocument(AxisGroupConfigDocument document)
    {
        if (document is null)
        {
            throw new InvalidDataException("分组配置中包含空分组。");
        }

        if (document.Members is { Count: > 0 }
            && document.AxisAddresses is { Count: > 0 })
        {
            throw new InvalidDataException(
                $"分组 '{document.Id}' 同时包含 Members 和 AxisAddresses。");
        }

        List<AxisGroupMember> members;
        if (document.Members is { Count: > 0 })
        {
            members = document.Members.Select(MapFromMemberDocument).ToList();
        }
        else if (document.AxisAddresses is not null)
        {
            members = document.AxisAddresses
                .Select(address => new AxisGroupMember
                {
                    Address = address,
                    Role = AxisRole.None
                })
                .ToList();
        }
        else if (document.Members is not null)
        {
            members = [];
        }
        else
        {
            throw new InvalidDataException(
                $"分组 '{document.Id}' 的轴成员集合缺失。");
        }

        return new AxisGroupConfig
        {
            Id = document.Id!,
            Name = document.Name!,
            Members = members
        };
    }

    private static AxisGroupConfigDocument MapToDocument(AxisGroupConfig group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Members = group.Members.Select(MapToMemberDocument).ToList()
    };

    private static AxisGroupMember MapFromMemberDocument(
        AxisGroupMemberDocument document)
    {
        if (document is null)
        {
            throw new InvalidDataException("分组配置中包含空轴成员。");
        }

        if (document.Address is null)
        {
            throw new InvalidDataException("分组配置中的轴成员缺少 Address。");
        }

        return new AxisGroupMember
        {
            Address = document.Address.Value,
            Role = document.Role
        };
    }

    private static AxisGroupMemberDocument MapToMemberDocument(AxisGroupMember member) => new()
    {
        Address = member.Address,
        Role = member.Role
    };

    private static AxisGroupMember CloneMember(AxisGroupMember member) => new()
    {
        Address = member.Address,
        Role = member.Role
    };

    private static string Format(AxisAddress address) =>
        $"{address.CardNo}/{address.AxisNo}";

    private sealed record AxisGroupConfigDocument
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public List<AxisGroupMemberDocument>? Members { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AxisAddress>? AxisAddresses { get; init; }
    }

    private sealed record AxisGroupMemberDocument
    {
        public AxisAddress? Address { get; init; }

        public AxisRole Role { get; init; }
    }
}
