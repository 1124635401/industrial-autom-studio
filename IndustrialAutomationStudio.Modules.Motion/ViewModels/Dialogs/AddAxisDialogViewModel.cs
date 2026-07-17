using System.Runtime.CompilerServices;
using IndustrialAutomationStudio.Modules.Motion.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.Dialogs;

public sealed class AddAxisDialogViewModel : BindableBase
{
    private int _cardNo;
    private int _axisNo;
    private string _axisName = "Axis0";
    private string _axisType = "DS402";

    public AddAxisDialogViewModel()
        : this(new AxisAddress(0, 0), "Axis0")
    {
    }

    public AddAxisDialogViewModel(AxisAddress suggestedAddress, string suggestedName)
    {
        _cardNo = suggestedAddress.CardNo;
        _axisNo = suggestedAddress.AxisNo;
        _axisName = suggestedName;
        ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
        CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(false));
    }

    public int CardNo
    {
        get => _cardNo;
        set => SetInput(ref _cardNo, value);
    }

    public int AxisNo
    {
        get => _axisNo;
        set => SetInput(ref _axisNo, value);
    }

    public string AxisName
    {
        get => _axisName;
        set => SetInput(ref _axisName, value ?? string.Empty);
    }

    public string AxisType
    {
        get => _axisType;
        set => SetInput(ref _axisType, value ?? string.Empty);
    }

    public IReadOnlyList<string> AxisTypes { get; } = ["DS402", "Pulse", "Virtual"];
    public AxisConfig? Result { get; private set; }
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public event Action<bool>? RequestClose;

    private bool CanConfirm() =>
        CardNo >= 0
        && AxisNo >= 0
        && !string.IsNullOrWhiteSpace(AxisName)
        && AxisName.Length <= 64
        && !string.IsNullOrWhiteSpace(AxisType);

    private void Confirm()
    {
        Result = AxisConfig.CreateDefault(new AxisAddress(CardNo, AxisNo), AxisName.Trim()) with
        {
            AxisType = AxisType
        };
        RequestClose?.Invoke(true);
    }

    private void SetInput<T>(
        ref T storage,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref storage, value, propertyName))
        {
            ConfirmCommand.RaiseCanExecuteChanged();
        }
    }
}
