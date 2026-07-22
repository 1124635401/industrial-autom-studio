using System.Runtime.InteropServices;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

[StructLayout(LayoutKind.Sequential)]
internal struct LctM60SlaveResource
{
    public int SlaveCount;
    public int AxisCount;
    public int IoSlaveCount;
    public int DigitalInputCount;
    public int DigitalOutputCount;
    public int AnalogInputCount;
    public int AnalogOutputCount;
    public int InputVariableCount;
    public int OutputVariableCount;
}
