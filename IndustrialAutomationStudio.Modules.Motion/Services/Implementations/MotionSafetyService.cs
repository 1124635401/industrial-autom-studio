using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class MotionSafetyService(IMotionCardService motionCardService)
    : IMotionSafetyService
{
    public bool CanConfigureAxis(out string reason)
    {
        if (!motionCardService.IsConnected)
        {
            reason = "运动控制卡尚未连接。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool IsEmergencyStopActive() => false;
    public bool HasAnyAxisAlarm() => false;
}
