using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IAxisConfigValidator
{
    AxisConfigValidationResult Validate(AxisConfig config);
}
