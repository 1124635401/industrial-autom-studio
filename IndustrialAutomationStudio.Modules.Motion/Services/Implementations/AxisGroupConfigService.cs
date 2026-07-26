using System.IO;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class AxisGroupConfigService : IAxisGroupConfigService
{
    private readonly IAxisGroupConfigRepository _repository;

    public AxisGroupConfigService(IAxisGroupConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AxisGroupConfig>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return groups.Select(Clone).ToArray();
    }

    public string? ValidateName(
        string name,
        IEnumerable<AxisGroupConfig> groups,
        string? currentGroupId = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(groups);
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0)
        {
            return "分组名称不能为空。";
        }

        if (normalizedName.Length > 50)
        {
            return "分组名称不能超过 50 个字符。";
        }

        return groups.Any(group =>
                   !string.Equals(group.Id, currentGroupId, StringComparison.Ordinal)
                   && string.Equals(
                       group.Name.Trim(),
                       normalizedName,
                       StringComparison.OrdinalIgnoreCase))
            ? "分组名称已存在。"
            : null;
    }

    public async Task SaveAsync(
        IReadOnlyCollection<AxisGroupConfig> groups,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var normalized = new List<AxisGroupConfig>(groups.Count);
        foreach (var group in groups)
        {
            var validationMessage = ValidateName(group.Name, groups, group.Id);
            if (validationMessage is not null)
            {
                throw new InvalidDataException(validationMessage);
            }

            normalized.Add(new AxisGroupConfig
            {
                Id = group.Id,
                Name = group.Name.Trim(),
                Members = NormalizeMembers(group)
            });
        }

        await _repository.SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
    }

    private static AxisGroupConfig Clone(AxisGroupConfig group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Members = group.Members.Select(CloneMember).ToList()
    };

    private static List<AxisGroupMember> NormalizeMembers(AxisGroupConfig group)
    {
        if (group.Members is null)
        {
            throw new InvalidDataException($"分组 '{group.Name}' 的轴成员集合缺失。");
        }

        var addresses = new HashSet<AxisAddress>();
        var explicitRoles = new Dictionary<AxisRole, AxisGroupMember>();
        foreach (var member in group.Members)
        {
            if (member is null)
            {
                throw new InvalidDataException($"分组 '{group.Name}' 中包含空轴成员。");
            }

            if (!addresses.Add(member.Address))
            {
                throw new InvalidDataException(
                    $"分组 '{group.Name}' 的轴地址 {Format(member.Address)} 重复。");
            }

            if (!Enum.IsDefined(member.Role))
            {
                throw new InvalidDataException(
                    $"分组 '{group.Name}' 包含未知轴角色 '{(int)member.Role}'。");
            }

            if (member.Role != AxisRole.None
                && !explicitRoles.TryAdd(member.Role, member))
            {
                var existing = explicitRoles[member.Role];
                throw new InvalidDataException(
                    $"分组 '{group.Name}' 的角色 {member.Role} 同时分配给 "
                    + $"{Format(existing.Address)} 和 {Format(member.Address)}。");
            }
        }

        return group.Members
            .OrderBy(member => member.Address.CardNo)
            .ThenBy(member => member.Address.AxisNo)
            .Select(CloneMember)
            .ToList();
    }

    private static AxisGroupMember CloneMember(AxisGroupMember member) => new()
    {
        Address = member.Address,
        Role = member.Role
    };

    private static string Format(AxisAddress address) =>
        $"{address.CardNo}/{address.AxisNo}";
}
