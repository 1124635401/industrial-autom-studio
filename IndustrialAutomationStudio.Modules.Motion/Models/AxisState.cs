namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record AxisState
{
    public AxisAddress Address { get; init; }
    public string AxisName { get; init; } = string.Empty;
    public double CommandPosition { get; init; }
    public double ActualPosition { get; init; }
    public double CurrentVelocity { get; init; }
    public bool ServoOn { get; init; }
    public bool IsMoving { get; init; }
    public bool IsHomed { get; init; }
    public bool Alarm { get; init; }
    public string AlarmMessage { get; init; } = string.Empty;
    public bool PositiveLimit { get; init; }
    public bool NegativeLimit { get; init; }
    public bool HomeSensor { get; init; }
    public bool InPosition { get; init; }
}
