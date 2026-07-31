using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

public sealed class AxisControlCardViewModel : BindableBase
{
    private readonly Func<bool, CancellationToken, Task>? _setServoEnabled;
    private AxisMotionMode _selectedMode;
    private double _position;
    private bool _servoOn;
    private bool _negativeLimit;
    private bool _homeSensor;
    private bool _positiveLimit;
    private bool _alarm;
    private bool _isMoving;
    private bool _inPosition;
    private double _jogSpeed;
    private double _relativeDistance = 10;
    private double _targetPosition;
    private double _motionSpeed;
    private bool _reportedServoOn;
    private bool _isServoCommandPending;
    private string? _servoCommandError;

    public AxisControlCardViewModel(
        AxisConfig config,
        AxisRole role,
        string axisLabel,
        string unit,
        Func<bool, CancellationToken, Task>? setServoEnabled = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Role = role;
        AxisLabel = axisLabel;
        Unit = unit;
        _setServoEnabled = setServoEnabled;
        SpeedUnit = string.IsNullOrWhiteSpace(unit) ? "/s" : $"{unit}/s";
        _jogSpeed = DefaultSpeed(config, 10);
        _motionSpeed = DefaultSpeed(config, 50);
        SelectModeCommand = new DelegateCommand<AxisMotionMode?>(mode =>
        {
            if (mode is not null)
            {
                SelectedMode = mode.Value;
            }
        });
        ToggleServoCommand = new AsyncDelegateCommand<bool?>(
            async (enabled, cancellationToken) =>
            {
                if (enabled.HasValue)
                {
                    await ToggleServoAsync(enabled.Value, cancellationToken);
                }
            },
            enabled => enabled.HasValue &&
                       _setServoEnabled is not null &&
                       !IsServoCommandPending);
    }

    public AxisConfig Config { get; }
    public AxisAddress Address => Config.Address;
    public AxisRole Role { get; }
    public string AxisLabel { get; }
    public string Unit { get; }
    public string SpeedUnit { get; }
    public string AxisNumberText => $"轴号 {Address.AxisNo}";

    public DelegateCommand<AxisMotionMode?> SelectModeCommand { get; }
    public AsyncDelegateCommand<bool?> ToggleServoCommand { get; }

    public AxisMotionMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (!SetProperty(ref _selectedMode, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(IsContinuousMode));
            RaisePropertyChanged(nameof(IsRelativeMode));
            RaisePropertyChanged(nameof(IsAbsoluteMode));
        }
    }

    public bool IsContinuousMode => SelectedMode == AxisMotionMode.Continuous;
    public bool IsRelativeMode => SelectedMode == AxisMotionMode.Relative;
    public bool IsAbsoluteMode => SelectedMode == AxisMotionMode.Absolute;

    public double Position
    {
        get => _position;
        private set => SetProperty(ref _position, value);
    }

    public bool ServoOn
    {
        get => _servoOn;
        set => SetStateProperty(ref _servoOn, value);
    }

    public bool IsServoCommandPending
    {
        get => _isServoCommandPending;
        private set
        {
            if (SetProperty(ref _isServoCommandPending, value))
            {
                RaisePropertyChanged(nameof(StateText));
                ToggleServoCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ServoCommandError
    {
        get => _servoCommandError;
        private set
        {
            if (SetProperty(ref _servoCommandError, value))
            {
                RaisePropertyChanged(nameof(StateText));
            }
        }
    }

    public bool NegativeLimit
    {
        get => _negativeLimit;
        private set => SetStateProperty(ref _negativeLimit, value);
    }

    public bool HomeSensor
    {
        get => _homeSensor;
        private set => SetStateProperty(ref _homeSensor, value);
    }

    public bool PositiveLimit
    {
        get => _positiveLimit;
        private set => SetStateProperty(ref _positiveLimit, value);
    }

    public bool Alarm
    {
        get => _alarm;
        private set => SetStateProperty(ref _alarm, value);
    }

    public bool IsMoving
    {
        get => _isMoving;
        private set => SetStateProperty(ref _isMoving, value);
    }

    public bool InPosition
    {
        get => _inPosition;
        private set => SetStateProperty(ref _inPosition, value);
    }

    public string StateText => IsServoCommandPending
        ? "使能切换中"
        : !string.IsNullOrWhiteSpace(ServoCommandError)
            ? ServoCommandError
            : Alarm
                ? "轴报警"
                : !ServoOn
                    ? "未使能"
                    : IsMoving
                        ? "运动中"
                        : "运行就绪";

    public double JogSpeed
    {
        get => _jogSpeed;
        set => SetProperty(ref _jogSpeed, value);
    }

    public double RelativeDistance
    {
        get => _relativeDistance;
        set => SetProperty(ref _relativeDistance, value);
    }

    public double TargetPosition
    {
        get => _targetPosition;
        set => SetProperty(ref _targetPosition, value);
    }

    public double MotionSpeed
    {
        get => _motionSpeed;
        set => SetProperty(ref _motionSpeed, value);
    }

    public void ApplyState(AxisState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Address != Address)
        {
            throw new ArgumentException("轴状态地址与轴卡不一致。", nameof(state));
        }

        Position = state.ActualPosition;
        _reportedServoOn = state.ServoOn;
        ServoOn = state.ServoOn;
        NegativeLimit = state.NegativeLimit;
        HomeSensor = state.HomeSensor;
        PositiveLimit = state.PositiveLimit;
        Alarm = state.Alarm;
        IsMoving = state.IsMoving;
        InPosition = state.InPosition;
    }

    public async Task ToggleServoAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (_setServoEnabled is null || IsServoCommandPending)
        {
            ServoOn = _reportedServoOn;
            return;
        }

        IsServoCommandPending = true;
        ServoCommandError = null;
        try
        {
            await _setServoEnabled(enabled, cancellationToken).ConfigureAwait(false);
            _reportedServoOn = enabled;
            ServoOn = enabled;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ServoOn = _reportedServoOn;
            ServoCommandError = exception.Message;
        }
        finally
        {
            IsServoCommandPending = false;
        }
    }

    private void SetStateProperty(ref bool field, bool value)
    {
        if (SetProperty(ref field, value))
        {
            RaisePropertyChanged(nameof(StateText));
        }
    }

    private static double DefaultSpeed(AxisConfig config, double preferred) =>
        double.IsFinite(config.MaxVelocity) && config.MaxVelocity > 0
            ? Math.Min(preferred, config.MaxVelocity)
            : preferred;
}
