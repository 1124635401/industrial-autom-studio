namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record PointAxisPosition
{
    public AxisAddress Address { get; init; }
    public double Position { get; init; }
}
