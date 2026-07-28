using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record WorkbenchShortcutViewModel(
    string TargetKey,
    string Title,
    string BadgeText,
    NavigationItem Target);
