using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IAxisGroupPromptService
{
    Task<ConfigurationPromptResult> ConfirmUnsavedChangesAsync();

    Task<bool> ConfirmDeleteAsync(string groupName);
}
