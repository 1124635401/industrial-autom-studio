namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IConfigurationFileDialogService
{
    Task<string?> SelectImportFileAsync();
    Task<string?> SelectExportFileAsync();
}
