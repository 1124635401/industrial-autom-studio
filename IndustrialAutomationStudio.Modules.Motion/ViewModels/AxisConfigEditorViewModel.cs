using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Implementations;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class AxisConfigEditorViewModel : BindableBase, INotifyDataErrorInfo
{
    private AxisConfig _accepted;
    private readonly IAxisConfigurationValidator _validator;
    private readonly Dictionary<string, string[]> _errors = [];
    private int _cardNo;
    private int _axisNo;
    private string _axisName;
    private string _axisType;
    private bool _isEnabled;
    private double _gearRatio;
    private double _resolution;
    private string _unit;
    private int _jogReverse;
    private double _maxVelocity;
    private double _acceleration;
    private double _deceleration;
    private double _sTime;
    private string _homeMode;
    private string _homeDirection;
    private double _homeAcceleration;
    private double _homeVelocity1;
    private double _homeVelocity2;
    private double _homeTimeout;
    private double _homeOffset;
    private double _inPositionError;
    private double _inPositionTimeout;
    private double _stopVelocityThreshold;
    private int _txPdoStart;
    private int _rxPdoStart;
    private bool _isDirty;

    public AxisConfigEditorViewModel(AxisConfig config)
        : this(config, new AxisConfigurationValidator())
    {
    }

    public AxisConfigEditorViewModel(
        AxisConfig config,
        IAxisConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(validator);
        _accepted = config;
        _validator = validator;
        _cardNo = config.Address.CardNo;
        _axisNo = config.Address.AxisNo;
        _axisName = config.AxisName;
        _axisType = config.AxisType;
        _isEnabled = config.IsEnabled;
        _gearRatio = config.GearRatio;
        _resolution = config.Resolution;
        _unit = config.Unit;
        _jogReverse = config.JogReverse;
        _maxVelocity = config.MaxVelocity;
        _acceleration = config.Acceleration;
        _deceleration = config.Deceleration;
        _sTime = config.STime;
        _homeMode = config.HomeMode;
        _homeDirection = config.HomeDirection;
        _homeAcceleration = config.HomeAcceleration;
        _homeVelocity1 = config.HomeVelocity1;
        _homeVelocity2 = config.HomeVelocity2;
        _homeTimeout = config.HomeTimeout;
        _homeOffset = config.HomeOffset;
        _inPositionError = config.InPositionError;
        _inPositionTimeout = config.InPositionTimeout;
        _stopVelocityThreshold = config.StopVelocityThreshold;
        _txPdoStart = config.TxPdoStart;
        _rxPdoStart = config.RxPdoStart;
        Revalidate();
    }

    public AxisAddress Address => new(CardNo, AxisNo);
    public int CardNo { get => _cardNo; set => SetEditable(ref _cardNo, value); }
    public int AxisNo { get => _axisNo; set => SetEditable(ref _axisNo, value); }
    public string DisplayName => IsDirty ? $"{AxisName} *" : AxisName;

    public string AxisName { get => _axisName; set => SetEditable(ref _axisName, value); }
    public string AxisType { get => _axisType; set => SetEditable(ref _axisType, value); }
    public bool IsEnabled { get => _isEnabled; set => SetEditable(ref _isEnabled, value); }
    public double GearRatio { get => _gearRatio; set => SetEditable(ref _gearRatio, value); }
    public double Resolution { get => _resolution; set => SetEditable(ref _resolution, value); }
    public string Unit { get => _unit; set => SetEditable(ref _unit, value); }
    public int JogReverse { get => _jogReverse; set => SetEditable(ref _jogReverse, value); }
    public double MaxVelocity { get => _maxVelocity; set => SetEditable(ref _maxVelocity, value); }
    public double Acceleration { get => _acceleration; set => SetEditable(ref _acceleration, value); }
    public double Deceleration { get => _deceleration; set => SetEditable(ref _deceleration, value); }
    public double STime { get => _sTime; set => SetEditable(ref _sTime, value); }
    public string HomeMode { get => _homeMode; set => SetEditable(ref _homeMode, value); }
    public string HomeDirection { get => _homeDirection; set => SetEditable(ref _homeDirection, value); }
    public double HomeAcceleration { get => _homeAcceleration; set => SetEditable(ref _homeAcceleration, value); }
    public double HomeVelocity1 { get => _homeVelocity1; set => SetEditable(ref _homeVelocity1, value); }
    public double HomeVelocity2 { get => _homeVelocity2; set => SetEditable(ref _homeVelocity2, value); }
    public double HomeTimeout { get => _homeTimeout; set => SetEditable(ref _homeTimeout, value); }
    public double HomeOffset { get => _homeOffset; set => SetEditable(ref _homeOffset, value); }
    public double InPositionError { get => _inPositionError; set => SetEditable(ref _inPositionError, value); }
    public double InPositionTimeout { get => _inPositionTimeout; set => SetEditable(ref _inPositionTimeout, value); }
    public double StopVelocityThreshold { get => _stopVelocityThreshold; set => SetEditable(ref _stopVelocityThreshold, value); }
    public int TxPdoStart { get => _txPdoStart; set => SetEditable(ref _txPdoStart, value); }
    public int RxPdoStart { get => _rxPdoStart; set => SetEditable(ref _rxPdoStart, value); }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool HasErrors => _errors.Count != 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(messages => messages);
        }

        return _errors.TryGetValue(propertyName, out var messages) ? messages : [];
    }

    public AxisConfig ToModel() => _accepted with
    {
        Address = new AxisAddress(CardNo, AxisNo),
        AxisName = AxisName,
        AxisType = AxisType,
        IsEnabled = IsEnabled,
        GearRatio = GearRatio,
        Resolution = Resolution,
        Unit = Unit,
        JogReverse = JogReverse,
        MaxVelocity = MaxVelocity,
        Acceleration = Acceleration,
        Deceleration = Deceleration,
        STime = STime,
        HomeMode = HomeMode,
        HomeDirection = HomeDirection,
        HomeAcceleration = HomeAcceleration,
        HomeVelocity1 = HomeVelocity1,
        HomeVelocity2 = HomeVelocity2,
        HomeTimeout = HomeTimeout,
        HomeOffset = HomeOffset,
        InPositionError = InPositionError,
        InPositionTimeout = InPositionTimeout,
        StopVelocityThreshold = StopVelocityThreshold,
        TxPdoStart = TxPdoStart,
        RxPdoStart = RxPdoStart
    };

    public void AcceptChanges()
    {
        _accepted = ToModel();
        IsDirty = false;
    }

    public void MarkDirty() => IsDirty = true;

    private void SetEditable<T>(
        ref T storage,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref storage, value, propertyName))
        {
            IsDirty = true;
            Revalidate();
            if (propertyName == nameof(AxisName))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    private void Revalidate()
    {
        var previousProperties = _errors.Keys.ToHashSet(StringComparer.Ordinal);
        var current = _validator.Validate(ToModel()).Errors
            .GroupBy(issue => issue.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(issue => issue.Message).ToArray(),
                StringComparer.Ordinal);
        var hadErrors = _errors.Count != 0;

        _errors.Clear();
        foreach (var pair in current)
        {
            _errors[pair.Key] = pair.Value;
        }

        foreach (var propertyName in previousProperties.Union(_errors.Keys))
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        if (hadErrors != HasErrors)
        {
            RaisePropertyChanged(nameof(HasErrors));
        }
    }
}
