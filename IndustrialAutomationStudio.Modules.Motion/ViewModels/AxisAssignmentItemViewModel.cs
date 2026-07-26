using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class AxisAssignmentItemViewModel : BindableBase
{
    private bool _isSelected;
    private AxisRole _role;
    private IReadOnlyList<AxisRole> _availableRoles = [];

    public AxisAssignmentItemViewModel(AxisConfig axis)
        : this(axis.Address, axis.AxisName, AxisRole.None)
    {
    }

    public AxisAssignmentItemViewModel(AxisConfig axis, AxisRole role)
        : this(axis.Address, axis.AxisName, role)
    {
    }

    public AxisAssignmentItemViewModel(
        AxisAddress address,
        string axisName,
        AxisRole role = AxisRole.None)
    {
        Address = address;
        AxisName = axisName;
        _role = role;
    }

    public AxisAddress Address { get; }

    public string AxisName { get; }

    public string AxisNumberText => $"Axis {Address.AxisNo}";

    public IReadOnlyList<AxisRole> AvailableRoles => _availableRoles;

    public AxisRole Role
    {
        get => _role;
        set
        {
            if (SetProperty(ref _role, value))
            {
                RaisePropertyChanged(nameof(RoleText));
            }
        }
    }

    public string RoleText => Role.ToString();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void SetAvailableRoles(IEnumerable<AxisRole> roles)
    {
        var updated = roles.ToArray();
        if (_availableRoles.SequenceEqual(updated))
        {
            return;
        }

        _availableRoles = updated;
        RaisePropertyChanged(nameof(AvailableRoles));
    }

    public AxisAssignmentItemViewModel Copy(AxisRole? role = null) =>
        new(Address, AxisName, role ?? Role);
}
