using IndustrialAutomationStudio.Modules.Motion.Navigation;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionWorkspaceViewModel : BindableBase, INavigationAware
{
    private readonly Action<
        string,
        string,
        NavigationParameters,
        Action<NavigationResult>> _requestNavigate;
    private readonly MotionModuleOptions _options;
    private string _activePage = MotionNavigationNames.Home;
    private string _activePageTitle = MotionNavigationDisplayNames.GetTitle(MotionNavigationNames.Home);

    public MotionWorkspaceViewModel(IRegionManager regionManager, MotionModuleOptions options)
        : this(
            (regionName, target, parameters, callback) => regionManager.RequestNavigate(
                regionName,
                target,
                callback,
                parameters),
            options)
    {
    }

    internal MotionWorkspaceViewModel(
        Action<
            string,
            string,
            NavigationParameters,
            Action<NavigationResult>> requestNavigate,
        MotionModuleOptions options)
    {
        _requestNavigate = requestNavigate;
        _options = options;
        NavigateHomeCommand = CreateNavigation(MotionNavigationNames.Home);
        NavigateConnectionCommand = CreateNavigation(MotionNavigationNames.Connection);
        NavigateAxisConfigCommand = CreateNavigation(MotionNavigationNames.AxisConfig);
        NavigateGroupManagementCommand = CreateNavigation(MotionNavigationNames.GroupManagement);
        NavigateAxisDebugCommand = CreateNavigation(MotionNavigationNames.AxisDebug, "单轴调试");
        NavigateIoMonitorCommand = CreateNavigation(MotionNavigationNames.IoMonitor, "IO 监控");
        NavigatePointDebugCommand = CreateNavigation(MotionNavigationNames.PointDebug, "点位调试");
        NavigateMultiAxisCommand = CreateNavigation(MotionNavigationNames.MultiAxis, "多轴运动");
        NavigateAlarmCommand = CreateNavigation(MotionNavigationNames.Alarm, "报警诊断");
        NavigateLogCommand = CreateNavigation(MotionNavigationNames.Log);
        NavigationItems =
        [
            Item(MotionNavigationNames.Home, "运动首页", "MotionIcon.Home", NavigateHomeCommand),
            Item(MotionNavigationNames.Connection, "控制卡连接", "MotionIcon.Connection", NavigateConnectionCommand),
            Item(MotionNavigationNames.AxisConfig, "轴配置", "MotionIcon.Axis", NavigateAxisConfigCommand),
            Item(MotionNavigationNames.GroupManagement, "分组管理", "MotionIcon.Group", NavigateGroupManagementCommand),
            Item(MotionNavigationNames.AxisDebug, "单轴调试", "MotionIcon.Motion", NavigateAxisDebugCommand),
            Item(MotionNavigationNames.IoMonitor, "IO 监控", "MotionIcon.Io", NavigateIoMonitorCommand),
            Item(MotionNavigationNames.PointDebug, "点位调试", "MotionIcon.Point", NavigatePointDebugCommand),
            Item(MotionNavigationNames.MultiAxis, "多轴运动", "MotionIcon.MultiAxis", NavigateMultiAxisCommand),
            Item(MotionNavigationNames.Alarm, "报警诊断", "MotionIcon.Alarm", NavigateAlarmCommand),
            Item(MotionNavigationNames.Log, "运动日志", "MotionIcon.Log", NavigateLogCommand)
        ];
    }

    public string ActivePage { get => _activePage; private set => SetProperty(ref _activePage, value); }
    public string ActivePageTitle { get => _activePageTitle; private set => SetProperty(ref _activePageTitle, value); }
    public string DriverLabel => $"Driver: {_options.DefaultDriverKey}";
    public string VersionText => $"v{typeof(MotionWorkspaceViewModel).Assembly.GetName().Version}";
    public DelegateCommand NavigateHomeCommand { get; }
    public DelegateCommand NavigateConnectionCommand { get; }
    public DelegateCommand NavigateAxisConfigCommand { get; }
    public DelegateCommand NavigateGroupManagementCommand { get; }
    public DelegateCommand NavigateAxisDebugCommand { get; }
    public DelegateCommand NavigateIoMonitorCommand { get; }
    public DelegateCommand NavigatePointDebugCommand { get; }
    public DelegateCommand NavigateMultiAxisCommand { get; }
    public DelegateCommand NavigateAlarmCommand { get; }
    public DelegateCommand NavigateLogCommand { get; }
    public IReadOnlyList<MotionWorkspaceNavigationItemViewModel> NavigationItems { get; }

    public void OnNavigatedTo(NavigationContext navigationContext) => Navigate(MotionNavigationNames.Home);
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private DelegateCommand CreateNavigation(string target, string? title = null) =>
        new(() => Navigate(target, title));

    private void Navigate(string target, string? title = null)
    {
        var parameters = new NavigationParameters();
        if (!string.IsNullOrWhiteSpace(title))
        {
            parameters.Add("title", title);
        }

        _requestNavigate(_options.WorkspaceRegionName, target, parameters, result =>
        {
            if (!result.Success)
            {
                return;
            }

            ActivePage = target;
            ActivePageTitle = MotionNavigationDisplayNames.GetTitle(target);
            foreach (var item in NavigationItems)
            {
                item.IsActive = string.Equals(
                    item.Route,
                    target,
                    StringComparison.Ordinal);
            }
        });
    }

    private static MotionWorkspaceNavigationItemViewModel Item(
        string route,
        string title,
        string iconKey,
        DelegateCommand command) =>
        new(
            route,
            title,
            iconKey,
            command,
            string.Equals(route, MotionNavigationNames.Home, StringComparison.Ordinal));
}
