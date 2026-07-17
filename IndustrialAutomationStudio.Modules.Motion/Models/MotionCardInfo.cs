namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record MotionCardInfo
{
    public int CardNo { get; init; }
    public string CardName { get; init; } = string.Empty;
    public string CardType { get; init; } = string.Empty;
    public string DriverKey { get; init; } = string.Empty;
    public string FirmwareVersion { get; init; } = string.Empty;
    public int AxisCount { get; init; }
    public int DiCount { get; init; }
    public int DoCount { get; init; }
}
