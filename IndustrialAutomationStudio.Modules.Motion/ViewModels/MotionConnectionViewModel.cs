using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionConnectionViewModel : BindableBase
{
    private readonly IMotionCardService _service;
    private readonly IMotionCardConfigRepository _configRepository;
    private bool _isConnected;
    private bool _isBusy;
    private string _statusMessage = "未连接";
    private string _cardName = string.Empty;
    private string _driverKey = string.Empty;
    private string _firmwareVersion = string.Empty;
    private int _axisCount;
    private int _diCount;
    private int _doCount;

    public MotionConnectionViewModel(
        IMotionCardService service,
        IMotionCardConfigRepository configRepository)
    {
        _service = service;
        _configRepository = configRepository;
        _service.ConnectionChanged += OnConnectionChanged;
        ConnectCommand = new AsyncDelegateCommand(ConnectAsync);
        DisconnectCommand = new AsyncDelegateCommand(DisconnectAsync);
        ScanCommand = new AsyncDelegateCommand(ScanAsync);
    }

    public bool IsConnected { get => _isConnected; private set => SetProperty(ref _isConnected, value); }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string CardName { get => _cardName; private set => SetProperty(ref _cardName, value); }
    public string DriverKey { get => _driverKey; private set => SetProperty(ref _driverKey, value); }
    public string FirmwareVersion { get => _firmwareVersion; private set => SetProperty(ref _firmwareVersion, value); }
    public int AxisCount { get => _axisCount; private set => SetProperty(ref _axisCount, value); }
    public int DiCount { get => _diCount; private set => SetProperty(ref _diCount, value); }
    public int DoCount { get => _doCount; private set => SetProperty(ref _doCount, value); }

    public AsyncDelegateCommand ConnectCommand { get; }
    public AsyncDelegateCommand DisconnectCommand { get; }
    public AsyncDelegateCommand ScanCommand { get; }

    private Task ConnectAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        var config = await _configRepository.LoadAsync(cancellationToken);
        await _service.ConnectAsync(config, cancellationToken);
        await RefreshInfoAsync(cancellationToken);
        StatusMessage = "连接成功";
    });

    private Task DisconnectAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        await _service.DisconnectAsync(cancellationToken);
        IsConnected = false;
        StatusMessage = "已断开";
    });

    private Task ScanAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        var axes = await _service.ScanAxesAsync(cancellationToken);
        AxisCount = axes.Count;
        StatusMessage = $"扫描完成，共 {axes.Count} 根轴";
    });

    private async Task RefreshInfoAsync(CancellationToken cancellationToken)
    {
        var info = await _service.GetCardInfoAsync(cancellationToken);
        IsConnected = true;
        CardName = info.CardName;
        DriverKey = info.DriverKey;
        FirmwareVersion = info.FirmwareVersion;
        AxisCount = info.AxisCount;
        DiCount = info.DiCount;
        DoCount = info.DoCount;
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnConnectionChanged(object? sender, bool connected) => IsConnected = connected;
}
