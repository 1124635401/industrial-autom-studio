using IndustrialAutomationStudio.App.Navigation;
using IndustrialAutomationStudio.Shell.Contracts.Navigation;
using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Threading;

namespace IndustrialAutomationStudio.App;

public sealed class ShellWindowViewModel : BindableBase
{
    private readonly IShellNavigationService _navigationService;
    private readonly IShellNavigationState _navigationState;
    private readonly TopMenuState _menuState;
    private string _activeModuleKey = string.Empty;
    private string _activeItemKey = string.Empty;
    private string _pageTitle = "工作台";
    private string _pageDescription = "查看平台模块入口与建设状态";
    private string _breadcrumb = "工作台";
    private string? _errorMessage;
    private readonly DateTime _startedAt = DateTime.Now;
    private readonly DispatcherTimer _clockTimer;

    public ShellWindowViewModel(
        INavigationRegistry registry,
        IShellNavigationService navigationService,
        IShellNavigationState navigationState)
        : this(
            registry,
            navigationService,
            navigationState,
            new TopMenuState())
    {
    }

    internal ShellWindowViewModel(
        INavigationRegistry registry,
        IShellNavigationService navigationService,
        IShellNavigationState navigationState,
        TopMenuState menuState)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(navigationState);
        ArgumentNullException.ThrowIfNull(menuState);

        _navigationService = navigationService;
        _navigationState = navigationState;
        _menuState = menuState;

        var modules = registry.Modules
            .Select(module => new ShellNavigationModuleViewModel(module))
            .ToArray();
        PrimaryModules = modules
            .Where(module => module.Model.Placement == NavigationPlacement.Primary)
            .ToArray();
        UtilityModules = modules
            .Where(module => module.Model.Placement == NavigationPlacement.Utility)
            .ToArray();

        NavigateItemCommand =
            new AsyncDelegateCommand<ShellNavigationItemViewModel>(
                NavigateItemCommandAsync);
        ActivateModuleCommand =
            new DelegateCommand<ShellNavigationModuleViewModel>(ActivateModule);
        CloseMenuCommand = new DelegateCommand(CloseMenu);

        _navigationState.Changed += NavigationStateOnChanged;
        _menuState.Changed += MenuStateOnChanged;
        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) =>
        {
            RaisePropertyChanged(nameof(SystemTimeText));
            RaisePropertyChanged(nameof(RuntimeText));
        };
        _clockTimer.Start();
        ApplyNavigationState();
    }

    public string Title => "Industrial Automation Studio 工业自动化调试平台";
    public string StatusText =>
        string.IsNullOrWhiteSpace(ErrorMessage) ? "平台运行正常" : "导航异常";
    public string VersionText => $"v{typeof(ShellWindowViewModel).Assembly.GetName().Version}";
    public string SystemTimeText => $"系统时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    public string RuntimeText => $"运行时间：{DateTime.Now - _startedAt:hh\\:mm\\:ss}";
    public IReadOnlyList<ShellNavigationModuleViewModel> PrimaryModules { get; }
    public IReadOnlyList<ShellNavigationModuleViewModel> UtilityModules { get; }
    public AsyncDelegateCommand<ShellNavigationItemViewModel> NavigateItemCommand { get; }
    public DelegateCommand<ShellNavigationModuleViewModel> ActivateModuleCommand { get; }
    public DelegateCommand CloseMenuCommand { get; }
    public string? OpenModuleKey => _menuState.OpenModuleKey;
    public string? PinnedModuleKey => _menuState.PinnedModuleKey;
    public string ActiveModuleKey => _activeModuleKey;
    public string ActiveItemKey => _activeItemKey;
    public string PageTitle => _pageTitle;
    public string PageDescription => _pageDescription;
    public string Breadcrumb => _breadcrumb;
    public string? ErrorMessage => _errorMessage;

    public Task EnterModuleAsync(
        string moduleKey,
        CancellationToken cancellationToken = default) =>
        _menuState.EnterAsync(moduleKey, cancellationToken);

    public Task LeaveMenuAsync(CancellationToken cancellationToken = default) =>
        _menuState.LeaveAsync(cancellationToken);

    public void PinModule(string moduleKey) => _menuState.Pin(moduleKey);

    public void CloseMenu() => _menuState.Close();

    public async Task<bool> NavigateAsync(
        ShellNavigationItemViewModel item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var success = await _navigationService.NavigateAsync(
            item.Model,
            cancellationToken);
        if (success)
        {
            _menuState.Close();
        }

        return success;
    }

    private async Task NavigateItemCommandAsync(
        ShellNavigationItemViewModel item,
        CancellationToken cancellationToken)
    {
        await NavigateAsync(item, cancellationToken);
    }

    private void ActivateModule(ShellNavigationModuleViewModel module)
    {
        if (module.DisplayMode == NavigationModuleDisplayMode.Direct)
        {
            NavigateItemCommand.Execute(module.DefaultItem);
            return;
        }

        _menuState.Pin(module.Key);
    }

    private void NavigationStateOnChanged(object? sender, EventArgs eventArgs) =>
        ApplyNavigationState();

    private void MenuStateOnChanged(object? sender, EventArgs eventArgs)
    {
        foreach (var module in PrimaryModules.Concat(UtilityModules))
        {
            module.IsMenuOpen = string.Equals(
                module.Key,
                _menuState.OpenModuleKey,
                StringComparison.Ordinal);
        }

        RaisePropertyChanged(nameof(OpenModuleKey));
        RaisePropertyChanged(nameof(PinnedModuleKey));
    }

    private void ApplyNavigationState()
    {
        var current = _navigationState.Current;
        SetProperty(ref _activeModuleKey, current.ModuleKey, nameof(ActiveModuleKey));
        SetProperty(ref _activeItemKey, current.ItemKey, nameof(ActiveItemKey));

        if (!string.IsNullOrWhiteSpace(current.ItemTitle))
        {
            SetProperty(ref _pageTitle, current.ItemTitle, nameof(PageTitle));
        }

        if (!string.IsNullOrWhiteSpace(current.Description))
        {
            SetProperty(
                ref _pageDescription,
                current.Description,
                nameof(PageDescription));
        }

        if (!string.IsNullOrWhiteSpace(current.Breadcrumb))
        {
            SetProperty(ref _breadcrumb, current.Breadcrumb, nameof(Breadcrumb));
        }

        SetProperty(ref _errorMessage, current.ErrorMessage, nameof(ErrorMessage));
        RaisePropertyChanged(nameof(StatusText));

        foreach (var module in PrimaryModules.Concat(UtilityModules))
        {
            module.IsActive = string.Equals(
                module.Key,
                current.ModuleKey,
                StringComparison.Ordinal);
            foreach (var item in module.Groups.SelectMany(group => group.Items))
            {
                item.IsActive = string.Equals(
                    item.Key,
                    current.ItemKey,
                    StringComparison.Ordinal);
            }
        }
    }
}
