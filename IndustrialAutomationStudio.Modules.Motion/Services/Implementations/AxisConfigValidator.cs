using System.Collections.Generic;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class AxisConfigValidator : IAxisConfigValidator
{
    private static readonly HashSet<string> ValidUnits = new(
        ["mm", "degree", "pulse"],
        StringComparer.OrdinalIgnoreCase);

    public AxisConfigValidationResult Validate(AxisConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var errors = new List<ValidationIssue>();

        AddIf(string.IsNullOrWhiteSpace(config.AxisName), "AxisName", "轴名称不能为空");
        AddIf(config.Address.CardNo < 0, "CardNo", "轴卡编号不能小于 0");
        AddIf(config.Address.AxisNo < 0, "AxisNo", "轴编号不能小于 0");
        AddIf(config.IsEnabled && string.IsNullOrWhiteSpace(config.AxisType), "AxisType", "启用轴必须选择轴类型");
        AddIf(config.GearRatio <= 0, "GearRatio", "齿轮比必须大于 0");
        AddIf(config.Resolution <= 0, "Resolution", "分辨率必须大于 0");
        AddIf(!ValidUnits.Contains(config.Unit), "Unit", "单位只能是 mm、degree 或 pulse");
        AddIf(config.JogReverse is not (1 or -1), "JogReverse", "点动方向只能是 1 或 -1");
        AddIf(config.MaxVelocity <= 0, "MaxVelocity", "最大速度必须大于 0");
        AddIf(config.Acceleration <= 0, "Acceleration", "加速度必须大于 0");
        AddIf(config.Deceleration <= 0, "Deceleration", "减速度必须大于 0");
        AddIf(config.STime < 0, "STime", "S 曲线时间不能小于 0");
        AddIf(string.IsNullOrWhiteSpace(config.HomeMode), "HomeMode", "回零模式不能为空");
        AddIf(config.HomeDirection is not ("Positive" or "Negative"), "HomeDirection", "回零方向必须是 Positive 或 Negative");
        AddIf(config.HomeAcceleration <= 0, "HomeAcceleration", "回零加速度必须大于 0");
        AddIf(config.HomeVelocity1 <= 0, "HomeVelocity1", "回零速度 1 必须大于 0");
        AddIf(config.HomeVelocity2 <= 0, "HomeVelocity2", "回零速度 2 必须大于 0");
        AddIf(config.HomeTimeout <= 0, "HomeTimeout", "回零超时时间必须大于 0");
        AddIf(config.InPositionError < 0, "InPositionError", "到位误差不能小于 0");
        AddIf(config.InPositionTimeout <= 0, "InPositionTimeout", "到位超时时间必须大于 0");
        AddIf(config.StopVelocityThreshold < 0, "StopVelocityThreshold", "停止速度阈值不能小于 0");
        AddIf(config.TxPdoStart < 0, "TxPdoStart", "TxPdoStart 不能小于 0");
        AddIf(config.RxPdoStart < 0, "RxPdoStart", "RxPdoStart 不能小于 0");

        return new AxisConfigValidationResult(errors);

        void AddIf(bool condition, string propertyName, string message)
        {
            if (condition)
            {
                errors.Add(new ValidationIssue(propertyName, message));
            }
        }
    }
}
