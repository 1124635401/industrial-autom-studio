using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Settings.Navigation;

public sealed class SettingsNavigationCatalog : INavigationContributor
{
    public NavigationModule CreateNavigationModule() =>
        new(
            "settings",
            "系统设置",
            "ShellIcon.Settings",
            50,
            NavigationPlacement.Utility,
            NavigationModuleDisplayMode.Utility,
            "settings.general",
            true,
            [
                new NavigationGroup(
                    "settings.platform",
                    "平台设置",
                    0,
                    true,
                    [
                        DevelopmentItem(
                            "settings.general",
                            "通用设置",
                            "配置平台显示、刷新与运行参数",
                            "ShellIcon.Settings",
                            0),
                        DevelopmentItem(
                            "settings.permissions",
                            "用户权限",
                            "管理用户、角色与调试权限",
                            "ShellIcon.User",
                            10),
                        DevelopmentItem(
                            "settings.plugins",
                            "插件管理",
                            "查看和管理平台扩展模块",
                            "ShellIcon.Plugins",
                            20),
                        DevelopmentItem(
                            "settings.about",
                            "关于平台",
                            "查看版本、许可和运行环境信息",
                            "ShellIcon.Info",
                            30)
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
