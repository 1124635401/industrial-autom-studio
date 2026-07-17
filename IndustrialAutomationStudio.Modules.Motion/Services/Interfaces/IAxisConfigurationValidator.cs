using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

public interface IAxisConfigurationValidator
{
    AxisConfigValidationResult Validate(AxisConfig config);
    AxisConfigValidationResult ValidateCollection(IEnumerable<AxisConfig> axes);
}
