namespace IndustrialAutomationStudio.Modules.Motion.Models;

public readonly record struct AxisPulseTarget(AxisAddress Address, int TargetPulses);
