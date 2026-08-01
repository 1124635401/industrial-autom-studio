namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record WorkflowItemViewModel(
    string Name,
    string Version,
    DateTime StartTime,
    bool IsRunning);

public sealed record WorkflowListSectionViewModel(
    string Key,
    string Title,
    IReadOnlyList<WorkflowItemViewModel> Items) : WorkbenchSectionViewModel(Key, Title);
