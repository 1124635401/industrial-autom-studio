using System.Windows;
using IndustrialAutomationStudio.Modules.Motion;
using IndustrialAutomationStudio.Modules.Motion.Navigation;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.App;

public partial class App : PrismApplication
{
    protected override Window CreateShell() => Container.Resolve<ShellWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterInstance(new MotionModuleOptions
        {
            HostRegionName = MotionRegionNames.HostContent,
            WorkspaceRegionName = MotionRegionNames.WorkspaceContent,
            DefaultDriverKey = "Mock"
        });
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) =>
        moduleCatalog.AddModule<MotionModule>();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Container.Resolve<IRegionManager>().RequestNavigate(
            MotionRegionNames.HostContent,
            MotionNavigationNames.Workspace);
    }
}
