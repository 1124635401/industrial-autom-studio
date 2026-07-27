using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

public sealed class AxisPositionReadoutViewModel(
    AxisAddress address,
    AxisRole role,
    string axisLabel,
    string unit) : BindableBase
{
    private double _position;

    public AxisAddress Address { get; } = address;
    public AxisRole Role { get; } = role;
    public string AxisLabel { get; } = axisLabel;
    public string Unit { get; } = unit;

    public double Position
    {
        get => _position;
        internal set => SetProperty(ref _position, value);
    }
}
