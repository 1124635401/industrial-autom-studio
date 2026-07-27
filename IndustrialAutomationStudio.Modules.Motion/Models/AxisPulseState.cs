namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record AxisPulseState
{
    public AxisAddress Address { get; init; }
    public double CommandPulses { get; init; }
    public double ActualPulses { get; init; }
    public double VelocityPulsesPerSecond { get; init; }
    public bool ServoOn { get; init; }
    public bool IsMoving { get; init; }
    public bool Alarm { get; init; }
    public bool PositiveLimit { get; init; }
    public bool NegativeLimit { get; init; }
    public bool HomeSensor { get; init; }
    public bool InPosition { get; init; }
    public bool EmergencyStop { get; init; }
}
