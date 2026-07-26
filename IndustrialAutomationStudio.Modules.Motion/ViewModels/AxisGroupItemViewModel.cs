using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class AxisGroupItemViewModel : BindableBase
{
    private string _name;
    private int _axisCount;
    private bool _isTransient;

    public AxisGroupItemViewModel(
        AxisGroupConfig snapshot,
        int axisCount,
        bool isTransient = false)
    {
        Snapshot = Clone(snapshot);
        Id = snapshot.Id;
        _name = snapshot.Name;
        _axisCount = axisCount;
        _isTransient = isTransient;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public int AxisCount
    {
        get => _axisCount;
        private set => SetProperty(ref _axisCount, value);
    }

    public AxisGroupConfig Snapshot { get; private set; }

    public bool IsTransient
    {
        get => _isTransient;
        private set => SetProperty(ref _isTransient, value);
    }

    public void Accept(AxisGroupConfig snapshot, int validAxisCount)
    {
        Snapshot = Clone(snapshot);
        Name = snapshot.Name;
        AxisCount = validAxisCount;
    }

    public void MarkPersisted() => IsTransient = false;

    private static AxisGroupConfig Clone(AxisGroupConfig group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Members = group.Members
            .Select(member => new AxisGroupMember
            {
                Address = member.Address,
                Role = member.Role
            })
            .ToList()
    };
}
