namespace IndustrialAutomationStudio.Modules.Motion.Hardware;

public sealed class MotionDriverException : Exception
{
    public MotionDriverException(
        string message,
        string driverKey,
        string operation,
        int? cardNo = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        DriverKey = driverKey;
        Operation = operation;
        CardNo = cardNo;
    }

    public string DriverKey { get; }
    public string Operation { get; }
    public int? CardNo { get; }
}
