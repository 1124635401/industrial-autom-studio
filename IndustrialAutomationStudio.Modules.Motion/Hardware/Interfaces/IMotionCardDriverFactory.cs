using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;

public interface IMotionCardDriverFactory
{
    string DriverKey { get; }
    IMotionCardDriver Create(MotionCardConfig config);
}
