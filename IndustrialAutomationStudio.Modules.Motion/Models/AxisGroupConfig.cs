namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record AxisGroupConfig
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Name { get; init; } = string.Empty;

    public List<AxisGroupMember> Members { get; init; } = [];
}
