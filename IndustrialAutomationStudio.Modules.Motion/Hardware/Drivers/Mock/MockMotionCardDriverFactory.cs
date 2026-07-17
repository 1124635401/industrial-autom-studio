using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.Mock;

public sealed class MockMotionCardDriverFactory : IMotionCardDriverFactory
{
    public string DriverKey => "Mock";

    public IMotionCardDriver Create(MotionCardConfig config) =>
        new MockMotionCardDriver(config);
}
