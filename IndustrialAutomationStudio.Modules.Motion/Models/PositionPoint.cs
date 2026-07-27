namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record PositionPoint
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string GroupId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double Speed { get; init; }
    public List<PointAxisPosition> AxisPositions { get; init; } = [];
}
