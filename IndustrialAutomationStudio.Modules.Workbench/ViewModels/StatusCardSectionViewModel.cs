namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public enum WorkbenchStatusKind
{
    Normal,
    Success,
    Warning,
    Error,
    Info
}

public sealed record StatusCardSectionViewModel(
    string Key,
    string Title,
    string IconKey,
    string MainValue,
    string SubText,
    string StatusText,
    WorkbenchStatusKind StatusKind) : WorkbenchSectionViewModel(Key, Title);
