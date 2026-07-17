using IndustrialAutomationStudio.Modules.Motion.Navigation;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionWorkspaceViewModel : BindableBase, INavigationAware
{
    private readonly IRegionManager _regionManager;
    private readonly MotionModuleOptions _options;
    private string _activePage = MotionNavigationNames.Home;

    public MotionWorkspaceViewModel(IRegionManager regionManager, MotionModuleOptions options)
    {
        _regionManager = regionManager;
        _options = options;
        NavigateHomeCommand = CreateNavigation(MotionNavigationNames.Home);
        NavigateConnectionCommand = CreateNavigation(MotionNavigationNames.Connection);
        NavigateAxisConfigCommand = CreateNavigation(MotionNavigationNames.AxisConfig);
        NavigateAxisDebugCommand = CreateNavigation(MotionNavigationNames.AxisDebug, "单轴调试");
        NavigateIoMonitorCommand = CreateNavigation(MotionNavigationNames.IoMonitor, "IO 监控");
        NavigatePointDebugCommand = CreateNavigation(MotionNavigationNames.PointDebug, "点位调试");
        NavigateMultiAxisCommand = CreateNavigation(MotionNavigationNames.MultiAxis, "多轴运动");
        NavigateAlarmCommand = CreateNavigation(MotionNavigationNames.Alarm, "报警诊断");
        NavigateLogCommand = CreateNavigation(MotionNavigationNames.Log);
    }

    public string ActivePage { get => _activePage; private set => SetProperty(ref _activePage, value); }
    public DelegateCommand NavigateHomeCommand { get; }
    public DelegateCommand NavigateConnectionCommand { get; }
    public DelegateCommand NavigateAxisConfigCommand { get; }
    public DelegateCommand NavigateAxisDebugCommand { get; }
    public DelegateCommand NavigateIoMonitorCommand { get; }
    public DelegateCommand NavigatePointDebugCommand { get; }
    public DelegateCommand NavigateMultiAxisCommand { get; }
    public DelegateCommand NavigateAlarmCommand { get; }
    public DelegateCommand NavigateLogCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext) => Navigate(MotionNavigationNames.Home);
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private DelegateCommand CreateNavigation(string target, string? title = null) =>
        new(() => Navigate(target, title));

    private void Navigate(string target, string? title = null)
    {
        ActivePage = target;
        var parameters = new NavigationParameters();
        if (!string.IsNullOrWhiteSpace(title))
        {
            parameters.Add("title", title);
        }

        _regionManager.RequestNavigate(_options.WorkspaceRegionName, target, parameters);
    }
}
