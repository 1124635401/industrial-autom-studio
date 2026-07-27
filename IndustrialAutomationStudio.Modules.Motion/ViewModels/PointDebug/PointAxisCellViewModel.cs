using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Mvvm;
using System.Globalization;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

public sealed class PointAxisCellViewModel(
    AxisAddress address,
    AxisRole role,
    string axisLabel,
    string unit,
    double position) : BindableBase
{
    private double _position = position;
    private string _positionText = position.ToString("G", CultureInfo.CurrentCulture);
    private bool _hasError;

    public AxisAddress Address { get; } = address;
    public AxisRole Role { get; } = role;
    public string AxisLabel { get; } = axisLabel;
    public string Unit { get; } = unit;

    public double Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                PositionText = value.ToString("G", CultureInfo.CurrentCulture);
            }
        }
    }

    public string PositionText
    {
        get => _positionText;
        set
        {
            if (!SetProperty(ref _positionText, value ?? string.Empty))
            {
                return;
            }

            if (TryParse(_positionText, out var parsed) && double.IsFinite(parsed))
            {
                _position = parsed;
                RaisePropertyChanged(nameof(Position));
                HasError = false;
            }
            else
            {
                HasError = true;
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public void RestorePosition(double value)
    {
        _position = value;
        _positionText = value.ToString("G", CultureInfo.CurrentCulture);
        RaisePropertyChanged(nameof(Position));
        RaisePropertyChanged(nameof(PositionText));
        HasError = false;
    }

    private static bool TryParse(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
