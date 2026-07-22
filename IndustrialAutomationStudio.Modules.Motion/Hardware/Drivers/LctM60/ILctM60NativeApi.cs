namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

internal interface ILctM60NativeApi
{
    short Open(short cardNo, short parameter);
    short Close(short cardNo);
    short SetEmergencyInputInverted(short value, short cardNo);
    short SetEmergencyAction(byte value, short cardNo);
    short ClearEmergency(short cardNo);
    short LoadEni(string path, short cardNo);
    short ResetFpga(short cardNo);
    short ConnectEtherCat(short option, short cardNo);
    short DisconnectEtherCat(short cardNo);
    short LoadParameters(string path, short cardNo);
    short GetSlaveResource(out LctM60SlaveResource resource, short cardNo);
}
