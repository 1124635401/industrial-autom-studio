namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public enum AlarmSeverity
{
    Warning,
    Error
}

public sealed record AlarmItemViewModel(
    DateTime Time,
    AlarmSeverity Severity,
    string Source,
    string Message);

public sealed record AlarmListSectionViewModel(
    string Key,
    string Title,
    IReadOnlyList<AlarmItemViewModel> Items) : WorkbenchSectionViewModel(Key, Title);
