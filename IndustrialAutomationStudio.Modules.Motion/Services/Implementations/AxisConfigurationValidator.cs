using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class AxisConfigurationValidator : IAxisConfigurationValidator
{
    public AxisConfigValidationResult Validate(AxisConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var errors = new List<ValidationIssue>();

        AddIf(config.Address.CardNo < 0, nameof(config.Address.CardNo), "卡号不能小于 0");
        AddIf(config.Address.AxisNo < 0, nameof(config.Address.AxisNo), "轴号不能小于 0");
        AddIf(string.IsNullOrWhiteSpace(config.AxisName), nameof(config.AxisName), "轴名称不能为空");
        AddIf(config.AxisName.Length > 64, nameof(config.AxisName), "轴名称不能超过 64 个字符");
        AddIf(string.IsNullOrWhiteSpace(config.AxisType), nameof(config.AxisType), "轴类型不能为空");
        AddIf(config.GearRatio <= 0, nameof(config.GearRatio), "齿轮比必须大于 0");
        AddIf(config.Resolution <= 0, nameof(config.Resolution), "分辨率必须大于 0");
        AddIf(config.MaxVelocity < 0, nameof(config.MaxVelocity), "最大速度不能小于 0");
        AddIf(config.Acceleration < 0, nameof(config.Acceleration), "加速度不能小于 0");
        AddIf(config.Deceleration < 0, nameof(config.Deceleration), "减速度不能小于 0");
        AddIf(config.STime < 0, nameof(config.STime), "S 曲线时间不能小于 0");
        AddIf(config.InPositionError < 0, nameof(config.InPositionError), "到位误差不能小于 0");
        AddIf(config.JogReverse is not (1 or -1), nameof(config.JogReverse), "点动方向只能是 1 或 -1");
        AddIf(config.HomeAcceleration < 0, nameof(config.HomeAcceleration), "回零加速度不能小于 0");
        AddIf(config.HomeVelocity1 < 0, nameof(config.HomeVelocity1), "回零速度 1 不能小于 0");
        AddIf(config.HomeVelocity2 < 0, nameof(config.HomeVelocity2), "回零速度 2 不能小于 0");
        AddIf(string.IsNullOrWhiteSpace(config.HomeMode), nameof(config.HomeMode), "回零模式不能为空");
        AddIf(config.TxPdoStart < 0, nameof(config.TxPdoStart), "Tx PDO 起始地址不能小于 0");
        AddIf(config.RxPdoStart < 0, nameof(config.RxPdoStart), "Rx PDO 起始地址不能小于 0");

        return new AxisConfigValidationResult(errors);

        void AddIf(bool condition, string propertyName, string message)
        {
            if (condition)
            {
                errors.Add(new ValidationIssue(propertyName, message));
            }
        }
    }

    public AxisConfigValidationResult ValidateCollection(IEnumerable<AxisConfig> axes)
    {
        ArgumentNullException.ThrowIfNull(axes);
        var materialized = axes.ToArray();
        var errors = materialized.SelectMany(axis => Validate(axis).Errors).ToList();

        foreach (var duplicate in materialized
                     .GroupBy(axis => axis.Address)
                     .Where(group => group.Count() > 1))
        {
            errors.Add(new ValidationIssue(
                nameof(AxisAddress.AxisNo),
                $"卡 {duplicate.Key.CardNo} 的轴号 {duplicate.Key.AxisNo} 重复"));
        }

        return new AxisConfigValidationResult(errors);
    }
}
