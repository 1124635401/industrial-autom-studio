using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class AxisConfigValidationException : Exception
{
    public AxisConfigValidationException(AxisConfigValidationResult result)
        : base("轴配置参数校验失败。")
    {
        Result = result;
    }

    public AxisConfigValidationResult Result { get; }
}
