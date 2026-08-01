using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.Jog;

public sealed class JogDirectionViewModel : BindableBase
{
    private bool _isActive;

    public JogDirectionViewModel(
        AxisAddress address,
        AxisRole role,
        int direction,
        string axisName,
        string label,
        string symbol)
    {
        Address = address;
        Role = role;
        Direction = direction;
        AxisName = axisName;
        Label = label;
        Symbol = symbol;
    }

    public AxisAddress Address { get; }
    public AxisRole Role { get; }
    public int Direction { get; }
    public string AxisName { get; }
    public string Label { get; }
    public string Symbol { get; }

    public bool IsActive
    {
        get => _isActive;
        internal set => SetProperty(ref _isActive, value);
    }
}
