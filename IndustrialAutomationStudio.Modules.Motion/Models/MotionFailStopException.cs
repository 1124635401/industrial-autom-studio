namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed class MotionFailStopException : Exception
{
    public MotionFailStopException(
        string message,
        Exception motionException,
        Exception stopException)
        : base(message, new AggregateException(motionException, stopException))
    {
        MotionException = motionException;
        StopException = stopException;
    }

    public Exception MotionException { get; }
    public Exception StopException { get; }
}
