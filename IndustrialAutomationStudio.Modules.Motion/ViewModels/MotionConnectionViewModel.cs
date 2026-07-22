using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionConnectionViewModel : BindableBase, INavigationAware
{
    private readonly IMotionCardService _service;
    private readonly IMotionCardConfigRepository _configRepository;
    private readonly IConfigurationFileDialogService _fileDialogs;
    private readonly Dispatcher _dispatcher;
    private MotionCardConfig _loadedConfig = new();
    private bool _initialized;
    private bool _isConnected;
    private bool _isBusy;
    private string _statusMessage = "未连接";
    private string _cardName = string.Empty;
    private string _driverKey = string.Empty;
    private string _firmwareVersion = string.Empty;
    private int _axisCount;
    private int _diCount;
    private int _doCount;
    private string _selectedDriverKey = "Mock";
    private int _cardNo;
    private string _eniFilePath = string.Empty;
    private string _slaveParameterFilePath = string.Empty;
    private int _emergencyInputInverted;
    private int _emergencyAction;
    private string _driverValidationMessage = string.Empty;
    private string _cardNoValidationMessage = string.Empty;
    private string _eniFileValidationMessage = string.Empty;
    private string _slaveParameterFileValidationMessage = string.Empty;
    private string _emergencyInputValidationMessage = string.Empty;
    private string _emergencyActionValidationMessage = string.Empty;

    public MotionConnectionViewModel(
        IMotionCardService service,
        IMotionCardConfigRepository configRepository,
        IConfigurationFileDialogService fileDialogs)
    {
        _service = service;
        _configRepository = configRepository;
        _fileDialogs = fileDialogs;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _service.ConnectionChanged += OnConnectionChanged;

        ConnectCommand = new AsyncDelegateCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new AsyncDelegateCommand(DisconnectAsync, CanDisconnect);
        ScanCommand = new AsyncDelegateCommand(ScanAsync, CanScan);
        SaveConfigurationCommand = new AsyncDelegateCommand(SaveConfigurationAsync, CanEdit);
        SelectEniFileCommand = new AsyncDelegateCommand(SelectEniFileAsync, CanEditM60);
        SelectSlaveParameterFileCommand = new AsyncDelegateCommand(
            SelectSlaveParameterFileAsync,
            CanEditM60);
    }

    public IReadOnlyList<string> AvailableDriverKeys => _service.AvailableDriverKeys;
    public IReadOnlyList<KeyValuePair<int, string>> EmergencyPolarityOptions { get; } =
    [
        new(0, "常开（0）"),
        new(1, "常闭（1）")
    ];

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                RaisePropertyChanged(nameof(CanEditConfiguration));
                RaiseCommandState();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(CanEditConfiguration));
                RaiseCommandState();
            }
        }
    }

    public bool CanEditConfiguration => _initialized && !IsBusy && !IsConnected;
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string CardName { get => _cardName; private set => SetProperty(ref _cardName, value); }
    public string DriverKey { get => _driverKey; private set => SetProperty(ref _driverKey, value); }
    public string FirmwareVersion { get => _firmwareVersion; private set => SetProperty(ref _firmwareVersion, value); }
    public int AxisCount { get => _axisCount; private set => SetProperty(ref _axisCount, value); }
    public int DiCount { get => _diCount; private set => SetProperty(ref _diCount, value); }
    public int DoCount { get => _doCount; private set => SetProperty(ref _doCount, value); }

    public string SelectedDriverKey
    {
        get => _selectedDriverKey;
        set
        {
            if (SetProperty(ref _selectedDriverKey, value ?? string.Empty))
            {
                RaisePropertyChanged(nameof(IsLctM60Selected));
                UpdateValidationMessages();
                RaiseCommandState();
            }
        }
    }

    public bool IsLctM60Selected => string.Equals(
        SelectedDriverKey,
        LctM60MotionCardDriver.Key,
        StringComparison.OrdinalIgnoreCase);

    public int CardNo
    {
        get => _cardNo;
        set
        {
            if (SetProperty(ref _cardNo, value))
            {
                UpdateValidationMessages();
            }
        }
    }

    public string EniFilePath
    {
        get => _eniFilePath;
        set
        {
            if (SetProperty(ref _eniFilePath, value ?? string.Empty))
            {
                UpdateValidationMessages();
            }
        }
    }

    public string SlaveParameterFilePath
    {
        get => _slaveParameterFilePath;
        set
        {
            if (SetProperty(ref _slaveParameterFilePath, value ?? string.Empty))
            {
                UpdateValidationMessages();
            }
        }
    }

    public int EmergencyInputInverted
    {
        get => _emergencyInputInverted;
        set
        {
            if (SetProperty(ref _emergencyInputInverted, value))
            {
                UpdateValidationMessages();
            }
        }
    }

    public int EmergencyAction
    {
        get => _emergencyAction;
        set
        {
            if (SetProperty(ref _emergencyAction, value))
            {
                UpdateValidationMessages();
            }
        }
    }

    public string DriverValidationMessage
    {
        get => _driverValidationMessage;
        private set => SetProperty(ref _driverValidationMessage, value);
    }

    public string CardNoValidationMessage
    {
        get => _cardNoValidationMessage;
        private set => SetProperty(ref _cardNoValidationMessage, value);
    }

    public string EniFileValidationMessage
    {
        get => _eniFileValidationMessage;
        private set => SetProperty(ref _eniFileValidationMessage, value);
    }

    public string SlaveParameterFileValidationMessage
    {
        get => _slaveParameterFileValidationMessage;
        private set => SetProperty(ref _slaveParameterFileValidationMessage, value);
    }

    public string EmergencyInputValidationMessage
    {
        get => _emergencyInputValidationMessage;
        private set => SetProperty(ref _emergencyInputValidationMessage, value);
    }

    public string EmergencyActionValidationMessage
    {
        get => _emergencyActionValidationMessage;
        private set => SetProperty(ref _emergencyActionValidationMessage, value);
    }

    public AsyncDelegateCommand ConnectCommand { get; }
    public AsyncDelegateCommand DisconnectCommand { get; }
    public AsyncDelegateCommand ScanCommand { get; }
    public AsyncDelegateCommand SaveConfigurationCommand { get; }
    public AsyncDelegateCommand SelectEniFileCommand { get; }
    public AsyncDelegateCommand SelectSlaveParameterFileCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        var config = await _configRepository.LoadAsync(cancellationToken);
        ApplyConfiguration(config);
        _initialized = true;
        IsConnected = _service.IsConnected;
        RaisePropertyChanged(nameof(CanEditConfiguration));
        RaiseCommandState();
        if (IsConnected)
        {
            await RefreshInfoAsync(cancellationToken);
        }

        StatusMessage = IsConnected ? "已连接" : "配置已加载，等待连接";
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = InitializeForNavigationAsync();

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    private async Task InitializeForNavigationAsync()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private Task ConnectAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        var config = await SaveCoreAsync(cancellationToken);
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

    private Task SaveConfigurationAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        await SaveCoreAsync(cancellationToken);
        StatusMessage = "连接配置已保存";
    });

    private Task SelectEniFileAsync(CancellationToken _) => ExecuteBusyAsync(async () =>
    {
        var path = await _fileDialogs.SelectEniFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            EniFilePath = path;
        }
    });

    private Task SelectSlaveParameterFileAsync(CancellationToken _) => ExecuteBusyAsync(async () =>
    {
        var path = await _fileDialogs.SelectSlaveParameterFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            SlaveParameterFilePath = path;
        }
    });

    private async Task<MotionCardConfig> SaveCoreAsync(CancellationToken cancellationToken)
    {
        ValidateEditor();
        var config = _loadedConfig with
        {
            CardNo = CardNo,
            DriverKey = SelectedDriverKey,
            EniFilePath = EniFilePath.Trim(),
            SlaveParameterFilePath = SlaveParameterFilePath.Trim(),
            EmergencyInputInverted = EmergencyInputInverted,
            EmergencyAction = EmergencyAction
        };
        await _configRepository.SaveAsync(config, cancellationToken);
        _loadedConfig = config;
        return config;
    }

    private void ValidateEditor()
    {
        UpdateValidationMessages();

        if (string.IsNullOrWhiteSpace(SelectedDriverKey))
        {
            throw new InvalidOperationException("请选择 Driver。");
        }

        if (CardNo < 0 || CardNo > short.MaxValue)
        {
            throw new InvalidOperationException("板卡号必须在 0 到 32767 之间。");
        }

        if (!IsLctM60Selected)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EniFilePath))
        {
            throw new InvalidOperationException("请选择 EtherCAT ENI 文件。");
        }

        if (string.IsNullOrWhiteSpace(SlaveParameterFilePath))
        {
            throw new InvalidOperationException("请选择从站参数文件。");
        }

        if (EmergencyInputInverted is not 0 and not 1)
        {
            throw new InvalidOperationException("急停输入极性只能为 0 或 1。");
        }

        if (EmergencyAction < byte.MinValue || EmergencyAction > byte.MaxValue)
        {
            throw new InvalidOperationException("急停动作必须在 0 到 255 之间。");
        }
    }

    private void UpdateValidationMessages()
    {
        DriverValidationMessage = string.IsNullOrWhiteSpace(SelectedDriverKey)
            ? "请选择 Driver。"
            : string.Empty;
        CardNoValidationMessage = CardNo < 0 || CardNo > short.MaxValue
            ? "板卡号必须在 0 到 32767 之间。"
            : string.Empty;

        if (!IsLctM60Selected)
        {
            EniFileValidationMessage = string.Empty;
            SlaveParameterFileValidationMessage = string.Empty;
            EmergencyInputValidationMessage = string.Empty;
            EmergencyActionValidationMessage = string.Empty;
            return;
        }

        EniFileValidationMessage = string.IsNullOrWhiteSpace(EniFilePath)
            ? "请选择 EtherCAT ENI 文件。"
            : string.Empty;
        SlaveParameterFileValidationMessage = string.IsNullOrWhiteSpace(SlaveParameterFilePath)
            ? "请选择从站参数文件。"
            : string.Empty;
        EmergencyInputValidationMessage = EmergencyInputInverted is not 0 and not 1
            ? "急停输入极性只能为 0 或 1。"
            : string.Empty;
        EmergencyActionValidationMessage = EmergencyAction < byte.MinValue || EmergencyAction > byte.MaxValue
            ? "急停动作必须在 0 到 255 之间。"
            : string.Empty;
    }

    private void ApplyConfiguration(MotionCardConfig config)
    {
        _loadedConfig = config;
        SelectedDriverKey = config.DriverKey;
        CardNo = config.CardNo;
        EniFilePath = config.EniFilePath;
        SlaveParameterFilePath = config.SlaveParameterFilePath;
        EmergencyInputInverted = config.EmergencyInputInverted;
        EmergencyAction = config.EmergencyAction;
    }

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

    private bool CanConnect() => _initialized && !IsBusy && !IsConnected;
    private bool CanDisconnect() => _initialized && !IsBusy && IsConnected;
    private bool CanScan() => _initialized && !IsBusy && IsConnected;
    private bool CanEdit() => CanEditConfiguration;
    private bool CanEditM60() => CanEditConfiguration && IsLctM60Selected;

    private void OnConnectionChanged(object? sender, bool connected)
    {
        if (_dispatcher.CheckAccess())
        {
            ApplyConnectionChanged(connected);
            return;
        }

        _ = _dispatcher.InvokeAsync(() => ApplyConnectionChanged(connected));
    }

    private void ApplyConnectionChanged(bool connected) => IsConnected = connected;

    private void RaiseCommandState()
    {
        ConnectCommand.RaiseCanExecuteChanged();
        DisconnectCommand.RaiseCanExecuteChanged();
        ScanCommand.RaiseCanExecuteChanged();
        SaveConfigurationCommand.RaiseCanExecuteChanged();
        SelectEniFileCommand.RaiseCanExecuteChanged();
        SelectSlaveParameterFileCommand.RaiseCanExecuteChanged();
    }
}
