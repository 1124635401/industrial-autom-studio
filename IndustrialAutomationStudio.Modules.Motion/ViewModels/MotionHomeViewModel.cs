using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionHomeViewModel : BindableBase
{
    private readonly IMotionCardService _cardService;
    private readonly IMotionLogService _logService;
    private bool _isConnected;
    private int _axisCount;
    private int _diCount;
    private int _doCount;
    private string _latestLog = "暂无日志";

    public MotionHomeViewModel(
        IMotionCardService cardService,
        IMotionLogService logService)
    {
        _cardService = cardService;
        _logService = logService;
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
    }

    public bool IsConnected { get => _isConnected; private set => SetProperty(ref _isConnected, value); }
    public int AxisCount { get => _axisCount; private set => SetProperty(ref _axisCount, value); }
    public int DiCount { get => _diCount; private set => SetProperty(ref _diCount, value); }
    public int DoCount { get => _doCount; private set => SetProperty(ref _doCount, value); }
    public int AlarmCount => 0;
    public string LatestLog { get => _latestLog; private set => SetProperty(ref _latestLog, value); }
    public AsyncDelegateCommand RefreshCommand { get; }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsConnected = _cardService.IsConnected;
        if (IsConnected)
        {
            var info = await _cardService.GetCardInfoAsync(cancellationToken);
            AxisCount = info.AxisCount;
            DiCount = info.DiCount;
            DoCount = info.DoCount;
        }

        LatestLog = _logService.Entries.LastOrDefault()?.Result ?? "暂无日志";
    }
}
