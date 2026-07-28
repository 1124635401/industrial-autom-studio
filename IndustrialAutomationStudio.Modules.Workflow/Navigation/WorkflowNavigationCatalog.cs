using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Workflow.Navigation;

public sealed class WorkflowNavigationCatalog : INavigationContributor
{
    public NavigationModule CreateNavigationModule() =>
        new(
            "workflow",
            "流程编排",
            "ShellIcon.Workflow",
            30,
            NavigationPlacement.Primary,
            NavigationModuleDisplayMode.Menu,
            "workflow.designer",
            true,
            [
                new NavigationGroup(
                    "workflow.design",
                    "流程设计",
                    0,
                    true,
                    [
                        DevelopmentItem(
                            "workflow.designer",
                            "节点流程",
                            "使用运动、IO、通讯和逻辑节点编排流程",
                            "ShellIcon.Workflow",
                            0),
                        DevelopmentItem(
                            "workflow.debug",
                            "流程调试",
                            "单步、连续运行并定位当前节点",
                            "ShellIcon.Debug",
                            10)
                    ]),
                new NavigationGroup(
                    "workflow.analysis",
                    "运行分析",
                    10,
                    true,
                    [
                        DevelopmentItem(
                            "workflow.variables",
                            "变量监控",
                            "查看流程输入、输出和运行变量",
                            "ShellIcon.Variables",
                            0),
                        DevelopmentItem(
                            "workflow.records",
                            "执行记录",
                            "查看流程运行结果与节点耗时",
                            "MotionIcon.Log",
                            10)
                    ])
            ]);

    private static NavigationItem DevelopmentItem(
        string key,
        string title,
        string description,
        string iconKey,
        int order) =>
        new(
            key,
            title,
            description,
            iconKey,
            ShellNavigationUris.DevelopmentPlaceholder,
            order,
            isDevelopment: true);
}
