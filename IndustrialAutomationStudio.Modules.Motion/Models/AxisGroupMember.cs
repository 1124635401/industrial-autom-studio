namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record AxisGroupMember
{
    public AxisAddress Address { get; init; }

    public AxisRole Role { get; init; }
}
