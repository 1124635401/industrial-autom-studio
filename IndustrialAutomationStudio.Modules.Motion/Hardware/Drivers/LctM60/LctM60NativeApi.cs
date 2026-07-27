using System.Runtime.InteropServices;

namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

internal sealed class LctM60NativeApi : ILctM60NativeApi
{
    private const string LibraryName = "ecat_motion.dll";

    public short Open(short cardNo, short parameter) => MOpen(cardNo, parameter);
    public short Close(short cardNo) => MClose(cardNo);
    public short SetEmergencyInputInverted(short value, short cardNo) => MSetEmgInv(value, cardNo);
    public short SetEmergencyAction(byte value, short cardNo) => MSetEmgAction(value, cardNo);
    public short ClearEmergency(short cardNo) => MClrEmg(cardNo);
    public short GetEmergencyStop(out short emergencyStop, short cardNo) =>
        MGetEmg(out emergencyStop, cardNo);
    public short LoadEni(string path, short cardNo) => MLoadEni(path, cardNo);
    public short ResetFpga(short cardNo) => MResetFpga(cardNo);
    public short ConnectEtherCat(short option, short cardNo) => MConnectEtherCat(option, cardNo);
    public short DisconnectEtherCat(short cardNo) => MDisconnectEtherCat(cardNo);
    public short LoadParameters(string path, short cardNo) => MLoadParameters(path, cardNo);
    public short GetSlaveResource(out LctM60SlaveResource resource, short cardNo) =>
        MGetSlaveResource(out resource, cardNo);
    public short GetStatus(short axisNo, out int status, short count, short cardNo) =>
        MGetStatus(axisNo, out status, count, cardNo);
    public short GetCommandPosition(
        short axisNo,
        out double position,
        short count,
        short cardNo) =>
        MGetCommandPosition(axisNo, out position, count, cardNo);
    public short GetEncoderPosition(
        short axisNo,
        out double position,
        short count,
        short cardNo) =>
        MGetEncoderPosition(axisNo, out position, count, cardNo);
    public short GetCommandVelocity(
        short axisNo,
        out double velocity,
        short count,
        short cardNo) =>
        MGetCommandVelocity(axisNo, out velocity, count, cardNo);
    public short SetMove(
        short axisNo,
        ref LctM60CommandParameters parameters,
        short cardNo) =>
        MSetMove(axisNo, ref parameters, cardNo);
    public short Jog(short axisNo, double velocity, short cardNo) =>
        MJog(axisNo, velocity, cardNo);
    public short AbsoluteMove(short axisNo, int position, double velocity, short cardNo) =>
        MAbsoluteMove(axisNo, position, velocity, cardNo);
    public short LineAll(
        short dimension,
        short[] axes,
        int[] positions,
        double acceleration,
        double velocity,
        short cardNo) =>
        MLineAll(dimension, axes, positions, acceleration, velocity, cardNo);
    public short Stop(ulong axisMask, short option, short cardNo) =>
        MStop(axisMask, option, cardNo);

    [DllImport(LibraryName, EntryPoint = "M_Open", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MOpen(short cardNo, short parameter);

    [DllImport(LibraryName, EntryPoint = "M_Close", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MClose(short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_SetEmgInv", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MSetEmgInv(short value, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_SetEmgAction", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MSetEmgAction(short value, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_ClrEmg", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MClrEmg(short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetEmg", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetEmg(out short emergencyStop, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_LoadEni", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern short MLoadEni(string path, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_ResetFpga", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MResetFpga(short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_ConnectECAT", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MConnectEtherCat(short option, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_DisconnectECAT", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MDisconnectEtherCat(short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_LoadParamFromFile", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern short MLoadParameters(string path, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetSlaveResource", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetSlaveResource(out LctM60SlaveResource resource, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetSts", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetStatus(
        short axisNo,
        out int status,
        short count,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetCmd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetCommandPosition(
        short axisNo,
        out double position,
        short count,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetEncPos", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetEncoderPosition(
        short axisNo,
        out double position,
        short count,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_GetCmdVel", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MGetCommandVelocity(
        short axisNo,
        out double velocity,
        short count,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_SetMove", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MSetMove(
        short axisNo,
        ref LctM60CommandParameters parameters,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_Jog", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MJog(short axisNo, double velocity, short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_AbsMove", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MAbsoluteMove(
        short axisNo,
        int position,
        double velocity,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_Line_All", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MLineAll(
        short dimension,
        [In] short[] axes,
        [In] int[] positions,
        double acceleration,
        double velocity,
        short cardNo);

    [DllImport(LibraryName, EntryPoint = "M_Stop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern short MStop(ulong axisMask, short option, short cardNo);
}
