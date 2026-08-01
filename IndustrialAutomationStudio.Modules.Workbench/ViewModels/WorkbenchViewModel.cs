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
            Section("control-card", "控制卡状态", "卡", "控制卡连接与状态尚未接入工作台"),
            Section("axes", "轴状态", "轴", "轴数量与运行状态尚未接入工作台"),
            Section("io", "IO 状态", "IO", "输入输出汇总尚未接入工作台"),
            Section("communication", "通讯状态", "讯", "通讯连接统计将在后续阶段提供"),
            Section("alarms", "报警状态", "警", "报警汇总将在诊断中心接入")
        ];
        OverviewSections =
        [
            Section("axis-overview", "轴状态概览", "轴", "实时轴位置与速度尚未接入"),
            Section("io-overview", "IO 状态概览", "IO", "实时输入输出点位尚未接入"),
            Section("system-resource", "系统资源", "资", "CPU、内存与磁盘趋势尚未接入")
        ];
        RecentSections =
        [
            Section("recent-alarm", "最近报警", "警", "报警历史尚未接入"),
            Section("recent-workflow", "最近运行流程", "流", "流程运行记录尚未接入")
        ];
        Sections = StatusCards
            .Concat(OverviewSections)
            .Concat(RecentSections)
            .ToArray();

        QuickEntries = ShortcutKeys
            .Select(registry.FindItem)
            .Where(item => item is not null)
            .Select(item => CreateShortcut(item!))
            .ToArray();

        NavigateShortcutCommand =
            new AsyncDelegateCommand<WorkbenchShortcutViewModel>(
                NavigateShortcutCommandAsync);
    }

    public IReadOnlyList<WorkbenchSectionViewModel> StatusCards { get; }
    public IReadOnlyList<WorkbenchSectionViewModel> OverviewSections { get; }
    public IReadOnlyList<WorkbenchSectionViewModel> RecentSections { get; }
    public IReadOnlyList<WorkbenchSectionViewModel> Sections { get; }
    public IReadOnlyList<WorkbenchShortcutViewModel> QuickEntries { get; }
    public AsyncDelegateCommand<WorkbenchShortcutViewModel> NavigateShortcutCommand { get; }

    public Task<bool> NavigateShortcutAsync(
        WorkbenchShortcutViewModel shortcut,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        return _navigationService.NavigateAsync(shortcut.Target, cancellationToken);
    }

    private async Task NavigateShortcutCommandAsync(
        WorkbenchShortcutViewModel shortcut,
        CancellationToken cancellationToken)
    {
        await NavigateShortcutAsync(shortcut, cancellationToken);
    }

    private static WorkbenchSectionViewModel Section(
        string key,
        string title,
        string badge,
        string description) =>
        new(key, title, badge, "开发中/未接入", description);

    private static WorkbenchShortcutViewModel CreateShortcut(NavigationItem item)
    {
        var badge = item.Title switch
        {
            "控制卡连接" => "卡",
            "点位调试" => "点",
            "IO 监控" => "IO",
            "通讯连接" => "讯",
            "节点流程" => "流",
            _ => "↗"
        };
        return new WorkbenchShortcutViewModel(item.Key, item.Title, badge, item);
    }
}
