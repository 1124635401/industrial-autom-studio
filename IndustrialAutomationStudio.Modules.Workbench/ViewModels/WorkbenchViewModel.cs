using System.Windows;
using System.Windows.Media;
using IndustrialAutomationStudio.Shell.Contracts.Navigation;
using Prism.Commands;

namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed class WorkbenchViewModel
{
    private static readonly string[] ShortcutKeys =
    [
        "motion.connection",
        "motion.point-debug",
        "motion.io-monitor",
        "motion.group-management",
        "communication.connection",
        "workflow.designer"
    ];

    private readonly IShellNavigationService _navigationService;

    public WorkbenchViewModel(
        INavigationRegistry registry,
        IShellNavigationService navigationService)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(navigationService);
        _navigationService = navigationService;

        StatusCards =
        [
            new StatusCardSectionViewModel(
                "control-card",
                "控制卡状态",
                "ShellIcon.Connection",
                "1 / 1",
                "已连接 / 总数",
                "正常",
                WorkbenchStatusKind.Normal),
            new StatusCardSectionViewModel(
                "axes",
                "轴状态",
                "MotionIcon.Axis",
                "8",
                "总轴数",
                "正常",
                WorkbenchStatusKind.Success),
            new StatusCardSectionViewModel(
                "io",
                "IO 状态",
                "MotionIcon.Io",
                "64 / 64",
                "输入 / 输出",
                "正常",
                WorkbenchStatusKind.Info),
            new StatusCardSectionViewModel(
                "communication",
                "通讯状态",
                "ShellIcon.Communication",
                "3 / 4",
                "已连接 / 总数",
                "正常",
                WorkbenchStatusKind.Normal),
            new StatusCardSectionViewModel(
                "alarms",
                "报警状态",
                "ShellIcon.Notification",
                "0",
                "当前报警",
                "正常",
                WorkbenchStatusKind.Error)
        ];

        AxisOverview = new AxisOverviewSectionViewModel(
            "axis-overview",
            "轴状态概览",
            [
                new AxisStatusItemViewModel("Axis1", "X轴", AxisEnableState.Enabled, 123.456, 0.000),
                new AxisStatusItemViewModel("Axis2", "Y轴", AxisEnableState.Enabled, 78.900, 0.000),
                new AxisStatusItemViewModel("Axis3", "Z轴", AxisEnableState.Enabled, -50.250, 0.000),
                new AxisStatusItemViewModel("Axis4", "U轴", AxisEnableState.Enabled, 200.000, 0.000),
                new AxisStatusItemViewModel("Axis5", "V轴", AxisEnableState.Paused, 0.000, 0.000),
                new AxisStatusItemViewModel("Axis6", "W轴", AxisEnableState.Enabled, 300.500, 0.000),
                new AxisStatusItemViewModel("Axis7", "A轴", AxisEnableState.Disabled, 0.000, 0.000),
                new AxisStatusItemViewModel("Axis8", "B轴", AxisEnableState.Disabled, 0.000, 0.000)
            ]);

        IoOverview = CreateIoOverview();

        SystemResource = new SystemResourceSectionViewModel(
            "system-resource",
            "系统资源",
            new ResourceMetricViewModel("CPU使用率", 18, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1677FF"))),
            new ResourceMetricViewModel("内存使用率", 32, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF20B26B"))),
            new ResourceMetricViewModel("磁盘使用率", 46, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5A623"))),
            CreateCpuHistory());

        QuickEntries = new QuickEntrySectionViewModel(
            "quick-entries",
            "快捷入口",
            ShortcutKeys
                .Select(registry.FindItem)
                .Where(item => item is not null)
                .Select(item => CreateQuickEntry(item!))
                .ToArray());

        AlarmList = new AlarmListSectionViewModel(
            "recent-alarm",
            "最近报警",
            [
                new AlarmItemViewModel(
                    new DateTime(2024, 5, 20, 10, 15, 23),
                    AlarmSeverity.Warning,
                    "Axis2",
                    "正限位触发"),
                new AlarmItemViewModel(
                    new DateTime(2024, 5, 20, 9, 45, 11),
                    AlarmSeverity.Error,
                    "Axis4",
                    "跟随误差超限")
            ]);

        WorkflowList = new WorkflowListSectionViewModel(
            "recent-workflow",
            "最近运行流程",
            [
                new WorkflowItemViewModel(
                    "产品检测流程",
                    "v1.2.3",
                    new DateTime(2024, 5, 20, 11, 25, 36),
                    true)
            ]);

        NavigateQuickEntryCommand =
            new AsyncDelegateCommand<QuickEntryItemViewModel>(
                NavigateQuickEntryCommandAsync);
    }

    public IReadOnlyList<StatusCardSectionViewModel> StatusCards { get; }
    public AxisOverviewSectionViewModel AxisOverview { get; }
    public IoOverviewSectionViewModel IoOverview { get; }
    public SystemResourceSectionViewModel SystemResource { get; }
    public QuickEntrySectionViewModel QuickEntries { get; }
    public AlarmListSectionViewModel AlarmList { get; }
    public WorkflowListSectionViewModel WorkflowList { get; }

    public AsyncDelegateCommand<QuickEntryItemViewModel> NavigateQuickEntryCommand { get; }

    public Task<bool> NavigateQuickEntryAsync(
        QuickEntryItemViewModel entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return _navigationService.NavigateAsync(entry.Target, cancellationToken);
    }

    private async Task NavigateQuickEntryCommandAsync(
        QuickEntryItemViewModel entry,
        CancellationToken cancellationToken)
    {
        await NavigateQuickEntryAsync(entry, cancellationToken);
    }

    private static IoOverviewSectionViewModel CreateIoOverview()
    {
        var random = new Random(42);
        var inputs = Enumerable.Range(0, 64)
            .Select(i => new IoStateItemViewModel(i, random.NextDouble() > 0.35))
            .ToArray();
        var outputs = Enumerable.Range(0, 64)
            .Select(i => new IoStateItemViewModel(i, random.NextDouble() > 0.45))
            .ToArray();
        return new IoOverviewSectionViewModel(
            "io-overview",
            "IO 状态概览",
            64,
            64,
            inputs,
            outputs);
    }

    private static IReadOnlyList<Point> CreateCpuHistory()
    {
        var values = new[] { 35.0, 42.0, 55.0, 48.0, 60.0, 52.0, 45.0, 50.0, 38.0 };
        return values
            .Select((v, i) => new Point(i * 40.0, 100.0 - v))
            .ToArray();
    }

    private static QuickEntryItemViewModel CreateQuickEntry(NavigationItem item)
    {
        var badge = item.Title switch
        {
            "控制卡连接" => "卡",
            "点位调试" => "点",
            "IO 监控" => "IO",
            "分组管理" => "组",
            "通讯连接" => "讯",
            "节点流程" => "流",
            _ => "↗"
        };
        return new QuickEntryItemViewModel(item.Key, item.Title, item.IconKey, badge, item);
    }
}
