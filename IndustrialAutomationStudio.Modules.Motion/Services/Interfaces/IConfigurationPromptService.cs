using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IConfigurationPromptService
{
    Task<ConfigurationPromptResult> ConfirmUnsavedChangesAsync();
    Task<bool> ConfirmDeleteAsync(AxisConfig axis);
    Task<AxisConfig?> ShowAddAxisAsync(AxisAddress suggestedAddress, string suggestedName);
}
