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
    public short LoadEni(string path, short cardNo) => MLoadEni(path, cardNo);
    public short ResetFpga(short cardNo) => MResetFpga(cardNo);
    public short ConnectEtherCat(short option, short cardNo) => MConnectEtherCat(option, cardNo);
    public short DisconnectEtherCat(short cardNo) => MDisconnectEtherCat(cardNo);
    public short LoadParameters(string path, short cardNo) => MLoadParameters(path, cardNo);
    public short GetSlaveResource(out LctM60SlaveResource resource, short cardNo) =>
        MGetSlaveResource(out resource, cardNo);

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
}
