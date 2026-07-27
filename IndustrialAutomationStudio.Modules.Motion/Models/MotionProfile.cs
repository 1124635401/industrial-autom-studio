namespace IndustrialAutomationStudio.Modules.Motion.Models;

public readonly record struct MotionProfile(
    double AccelerationPulsesPerSecondSquared,
    double DecelerationPulsesPerSecondSquared,
    int STimeMilliseconds);
