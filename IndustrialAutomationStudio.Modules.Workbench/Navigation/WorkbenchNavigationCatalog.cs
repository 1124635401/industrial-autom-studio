using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Workbench.Navigation;

public sealed class WorkbenchNavigationCatalog : INavigationContributor
{
    public NavigationModule CreateNavigationModule()
    {
        var home = new NavigationItem(
            "workbench.home",
            "工作台",
            "查看平台模块入口与建设状态",
            "ShellIcon.Workbench",
            WorkbenchNavigationNames.Home,
            0);

        return new NavigationModule(
            "workbench",
            "工作台",
            "ShellIcon.Workbench",
            0,
            NavigationPlacement.Primary,
            NavigationModuleDisplayMode.Direct,
            home.Key,
            true,
            [
                new NavigationGroup(
                    "workbench.overview",
                    "系统首页",
                    0,
                    true,
                    [home])
            ]);
    }
}
