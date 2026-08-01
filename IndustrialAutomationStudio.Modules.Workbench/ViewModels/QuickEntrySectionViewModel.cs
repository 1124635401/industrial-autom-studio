using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record QuickEntryItemViewModel(
    string TargetKey,
    string Title,
    string IconKey,
    string BadgeText,
    NavigationItem Target);

public sealed record QuickEntrySectionViewModel(
    string Key,
    string Title,
    IReadOnlyList<QuickEntryItemViewModel> Items) : WorkbenchSectionViewModel(Key, Title);
