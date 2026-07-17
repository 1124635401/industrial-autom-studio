namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IMotionSafetyService
{
    bool CanConfigureAxis(out string reason);
    bool IsEmergencyStopActive();
    bool HasAnyAxisAlarm();
}
