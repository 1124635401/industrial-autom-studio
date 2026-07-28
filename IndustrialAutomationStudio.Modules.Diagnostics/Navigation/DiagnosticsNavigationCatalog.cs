using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Diagnostics.Navigation;

public sealed class DiagnosticsNavigationCatalog : INavigationContributor
{
    private const string MotionLogNavigationUri = "MotionLog";

    public NavigationModule CreateNavigationModule() =>
        new(
            "diagnostics",
            "诊断中心",
            "ShellIcon.Diagnostics",
            40,
            NavigationPlacement.Primary,
            NavigationModuleDisplayMode.Menu,
            "diagnostics.alarm",
            true,
            [
                new NavigationGroup(
                    "diagnostics.alarm-group",
                    "报警诊断",
                    0,
                    true,
                    [
                        DevelopmentItem(
                            "diagnostics.alarm",
                            "报警诊断",
                            "查看当前与历史报警及处理建议",
                            "MotionIcon.Alarm",
                            0)
                    ]),
                new NavigationGroup(
                    "diagnostics.logs",
                    "日志",
                    10,
                    true,
                    [
                        new NavigationItem(
                            "diagnostics.motion-log",
                            "运动日志",
                            "查看运动控制操作与执行结果",
                            "MotionIcon.Log",
                            MotionLogNavigationUri,
                            0),
                        DevelopmentItem(
                            "diagnostics.communication-log",
                            "通讯日志",
                            "查看通讯连接、收发和异常记录",
                            "MotionIcon.Log",
                            10),
                        DevelopmentItem(
                            "diagnostics.system-log",
                            "系统日志",
                            "查看平台运行和后台异常记录",
                            "MotionIcon.Log",
                            20)
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
