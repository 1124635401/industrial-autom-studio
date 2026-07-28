using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Communication.Navigation;

public sealed class CommunicationNavigationCatalog : INavigationContributor
{
    public NavigationModule CreateNavigationModule() =>
        new(
            "communication",
            "通讯调试",
            "ShellIcon.Communication",
            20,
            NavigationPlacement.Primary,
            NavigationModuleDisplayMode.Menu,
            "communication.connection",
            true,
            [
                new NavigationGroup(
                    "communication.connections",
                    "连接管理",
                    0,
                    true,
                    [
                        DevelopmentItem(
                            "communication.connection",
                            "通讯连接",
                            "管理 TCP、串口、OPC UA 和 Modbus 连接",
                            "ShellIcon.Connection",
                            0),
                        DevelopmentItem(
                            "communication.configuration",
                            "通讯配置",
                            "维护不同协议的动态连接参数",
                            "ShellIcon.Settings",
                            10)
                    ]),
                new NavigationGroup(
                    "communication.data",
                    "数据调试",
                    10,
                    true,
                    [
                        DevelopmentItem(
                            "communication.monitor",
                            "数据监控",
                            "过滤、暂停并查看通讯收发数据",
                            "ShellIcon.Data",
                            0),
                        DevelopmentItem(
                            "communication.message",
                            "报文调试",
                            "发送文本或 HEX 报文并预览解析结果",
                            "ShellIcon.Message",
                            10),
                        DevelopmentItem(
                            "communication.log",
                            "通讯日志",
                            "查看通讯连接与收发记录",
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
