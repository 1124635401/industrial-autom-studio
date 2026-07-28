namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public sealed record ShellNavigationSnapshot(
    string ModuleKey,
    string ModuleTitle,
    string ItemKey,
    string ItemTitle,
    string Description,
    string Breadcrumb,
    string? ErrorMessage);
