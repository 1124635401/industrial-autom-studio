namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record IoStateItemViewModel(int Index, bool IsOn);

public sealed record IoOverviewSectionViewModel(
    string Key,
    string Title,
    int InputCount,
    int OutputCount,
    IReadOnlyList<IoStateItemViewModel> Inputs,
    IReadOnlyList<IoStateItemViewModel> Outputs) : WorkbenchSectionViewModel(Key, Title)
{
    public IReadOnlyList<int> IndexLabels { get; } = [0, 8, 16, 24, 32, 40, 48, 56];
}
