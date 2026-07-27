namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

internal interface ILctM60NativeApi
{
    short Open(short cardNo, short parameter);
    short Close(short cardNo);
    short SetEmergencyInputInverted(short value, short cardNo);
    short SetEmergencyAction(byte value, short cardNo);
    short ClearEmergency(short cardNo);
    short GetEmergencyStop(out short emergencyStop, short cardNo);
    short LoadEni(string path, short cardNo);
    short ResetFpga(short cardNo);
    short ConnectEtherCat(short option, short cardNo);
    short DisconnectEtherCat(short cardNo);
    short LoadParameters(string path, short cardNo);
    short GetSlaveResource(out LctM60SlaveResource resource, short cardNo);
    short GetStatus(short axisNo, out int status, short count, short cardNo);
    short GetCommandPosition(short axisNo, out double position, short count, short cardNo);
    short GetEncoderPosition(short axisNo, out double position, short count, short cardNo);
    short GetCommandVelocity(short axisNo, out double velocity, short count, short cardNo);
    short SetMove(
        short axisNo,
        ref LctM60CommandParameters parameters,
        short cardNo);
    short Jog(short axisNo, double velocity, short cardNo);
    short AbsoluteMove(short axisNo, int position, double velocity, short cardNo);
    short LineAll(
        short dimension,
        short[] axes,
        int[] positions,
        double acceleration,
        double velocity,
        short cardNo);
    short Stop(ulong axisMask, short option, short cardNo);
}
