using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

public sealed class LctM60MotionCardDriverFactory : IMotionCardDriverFactory
{
    public string DriverKey => LctM60MotionCardDriver.Key;

    public IMotionCardDriver Create(MotionCardConfig config) =>
        new LctM60MotionCardDriver(config);
}
