using System.Collections.ObjectModel;
using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;
using System.Globalization;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

public sealed class PointRowViewModel : BindableBase
{
    private string _name;
    private double _speed;
    private string _speedText;
    private bool _speedHasError;
    private bool _isEditing;
    private bool _isCompatible;
    private string _statusText = string.Empty;
    private bool _isRunning;
    private Snapshot? _snapshot;

    public PointRowViewModel(
        PositionPoint point,
        IEnumerable<PointAxisCellViewModel> axisCells,
        bool isNew = false)
    {
        Id = point.Id;
        GroupId = point.GroupId;
        _name = point.Name;
        _speed = point.Speed;
        _speedText = point.Speed.ToString("G", CultureInfo.CurrentCulture);
        AxisCells = [.. axisCells];
        IsNew = isNew;
        _isEditing = isNew;
        _isCompatible = true;
        if (isNew)
        {
            CaptureSnapshot();
        }
    }

    public string Id { get; }
    public string GroupId { get; }
    public ObservableCollection<PointAxisCellViewModel> AxisCells { get; }
    public bool IsNew { get; internal set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value ?? string.Empty);
    }

    public double Speed
    {
        get => _speed;
        set
        {
            if (SetProperty(ref _speed, value))
            {
                SpeedText = value.ToString("G", CultureInfo.CurrentCulture);
            }
        }
    }

    public string SpeedText
    {
        get => _speedText;
        set
        {
            if (!SetProperty(ref _speedText, value ?? string.Empty))
            {
                return;
            }

            if (TryParse(_speedText, out var parsed) &&
                double.IsFinite(parsed) &&
                parsed > 0)
            {
                _speed = parsed;
                RaisePropertyChanged(nameof(Speed));
                SpeedHasError = false;
            }
            else
            {
                SpeedHasError = true;
            }
        }
    }

    public bool SpeedHasError
    {
        get => _speedHasError;
        private set => SetProperty(ref _speedHasError, value);
    }

    public bool HasNumericErrors =>
        SpeedHasError || AxisCells.Any(cell => cell.HasError);

    public bool IsEditing
    {
        get => _isEditing;
        private set => SetProperty(ref _isEditing, value);
    }

    public bool IsCompatible
    {
        get => _isCompatible;
        private set => SetProperty(ref _isCompatible, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public void BeginEdit()
    {
        CaptureSnapshot();
        IsEditing = true;
    }

    public void CommitEdit()
    {
        IsEditing = false;
        IsNew = false;
        _snapshot = null;
    }

    public void CancelEdit()
    {
        if (_snapshot is not null)
        {
            Name = _snapshot.Name;
            RestoreSpeed(_snapshot.Speed);
            var values = _snapshot.Positions.ToDictionary(value => value.Address);
            foreach (var cell in AxisCells)
            {
                if (values.TryGetValue(cell.Address, out var saved))
                {
                    cell.RestorePosition(saved.Position);
                }
            }
        }

        IsEditing = false;
        _snapshot = null;
    }

    private void RestoreSpeed(double value)
    {
        _speed = value;
        _speedText = value.ToString("G", CultureInfo.CurrentCulture);
        RaisePropertyChanged(nameof(Speed));
        RaisePropertyChanged(nameof(SpeedText));
        SpeedHasError = false;
    }

    public void UpdateCompatibility(AxisGroupConfig group)
    {
        var groupAddresses = group.Members.Select(member => member.Address).ToHashSet();
        IsCompatible =
            string.Equals(GroupId, group.Id, StringComparison.Ordinal) &&
            groupAddresses.SetEquals(AxisCells.Select(cell => cell.Address));
        StatusText = IsCompatible ? "就绪" : "分组已变更";
    }

    public void MarkRunning()
    {
        IsRunning = true;
        StatusText = "运行中";
    }

    public void MarkCompleted()
    {
        IsRunning = false;
        StatusText = "完成";
    }

    public void MarkFailed(string reason)
    {
        IsRunning = false;
        StatusText = string.IsNullOrWhiteSpace(reason) ? "失败" : $"失败：{reason}";
    }

    public void MarkStopped()
    {
        IsRunning = false;
        StatusText = "已停止";
    }

    public PositionPoint ToModel() => new()
    {
        Id = Id,
        GroupId = GroupId,
        Name = Name.Trim(),
        Speed = Speed,
        AxisPositions = AxisCells
            .Select(cell => new PointAxisPosition
            {
                Address = cell.Address,
                Position = cell.Position
            })
            .ToList()
    };

    private void CaptureSnapshot() => _snapshot = new Snapshot(
        Name,
        Speed,
        AxisCells.Select(cell => new PointAxisPosition
        {
            Address = cell.Address,
            Position = cell.Position
        }).ToArray());

    private static bool TryParse(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private sealed record Snapshot(
        string Name,
        double Speed,
        IReadOnlyList<PointAxisPosition> Positions);
}
