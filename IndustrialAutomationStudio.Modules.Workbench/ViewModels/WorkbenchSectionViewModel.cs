namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record WorkbenchSectionViewModel(
    string Key,
    string Title,
    string BadgeText,
    string StatusText,
    string Description);
