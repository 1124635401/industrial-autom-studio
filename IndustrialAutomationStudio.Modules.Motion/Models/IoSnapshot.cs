namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record IoSnapshot(
    IReadOnlyList<bool?> DigitalInputs,
    IReadOnlyList<bool?> DigitalOutputs);
