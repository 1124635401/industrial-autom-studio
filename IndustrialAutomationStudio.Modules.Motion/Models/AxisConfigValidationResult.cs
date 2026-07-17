using System.Collections.Generic;

namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record ValidationIssue(string PropertyName, string Message);

public sealed class AxisConfigValidationResult
{
    public AxisConfigValidationResult(
        IReadOnlyList<ValidationIssue> errors,
        IReadOnlyList<ValidationIssue>? warnings = null)
    {
        Errors = errors;
        Warnings = warnings ?? [];
    }

    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationIssue> Errors { get; }
    public IReadOnlyList<ValidationIssue> Warnings { get; }
}
