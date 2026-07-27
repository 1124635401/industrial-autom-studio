using System.Runtime.InteropServices;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

[StructLayout(LayoutKind.Sequential)]
internal struct LctM60CommandParameters
{
    public int Acceleration;
    public int Deceleration;
    public int STime;
}
