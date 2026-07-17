namespace IndustrialAutomationStudio.Modules.Motion.Models;

public enum MotionLogLevel
{
    Information,
    Warning,
    Error
}

public sealed record MotionLogEntry(
    DateTimeOffset Timestamp,
    MotionLogLevel Level,
    string Module,
    string Target,
    string Operation,
    string Result,
    string? ExceptionMessage = null);
