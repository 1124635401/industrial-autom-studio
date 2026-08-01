namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public enum AxisEnableState
{
    Enabled,
    Paused,
    Disabled
}

public sealed record AxisStatusItemViewModel(
    string AxisId,
    string Name,
    AxisEnableState State,
    double Position,
    double Velocity);

public sealed record AxisOverviewSectionViewModel(
    string Key,
    string Title,
    IReadOnlyList<AxisStatusItemViewModel> Items) : WorkbenchSectionViewModel(Key, Title);
