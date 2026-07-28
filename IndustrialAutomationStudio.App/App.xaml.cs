using System.Windows;
using IndustrialAutomationStudio.App.Navigation;
using IndustrialAutomationStudio.App.ViewModels;
using IndustrialAutomationStudio.App.Views;
using IndustrialAutomationStudio.Modules.Communication;
using IndustrialAutomationStudio.Modules.Communication.Navigation;
using IndustrialAutomationStudio.Modules.Diagnostics;
using IndustrialAutomationStudio.Modules.Diagnostics.Navigation;
using IndustrialAutomationStudio.Modules.Motion;
using IndustrialAutomationStudio.Modules.Motion.Navigation;
using IndustrialAutomationStudio.Modules.Settings;
using IndustrialAutomationStudio.Modules.Settings.Navigation;
using IndustrialAutomationStudio.Modules.Workbench;
using IndustrialAutomationStudio.Modules.Workbench.Navigation;
using IndustrialAutomationStudio.Modules.Workflow;
using IndustrialAutomationStudio.Modules.Workflow.Navigation;
using IndustrialAutomationStudio.Shell.Contracts.Navigation;
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
        var navigationRegistry = new NavigationRegistry();
        INavigationContributor[] contributors =
        [
            new WorkbenchNavigationCatalog(),
            new MotionNavigationCatalog(),
            new CommunicationNavigationCatalog(),
            new WorkflowNavigationCatalog(),
            new DiagnosticsNavigationCatalog(),
            new SettingsNavigationCatalog()
        ];
        foreach (var contributor in contributors)
        {
            navigationRegistry.Register(contributor.CreateNavigationModule());
        }

        containerRegistry.RegisterInstance<INavigationRegistry>(navigationRegistry);
        containerRegistry.RegisterSingleton<IShellNavigationState, ShellNavigationState>();
        containerRegistry.RegisterSingleton<IShellNavigationService, ShellNavigationService>();
        containerRegistry.RegisterForNavigation<
            DevelopmentPlaceholderView,
            DevelopmentPlaceholderViewModel>(
            ShellNavigationUris.DevelopmentPlaceholder);

        containerRegistry.RegisterInstance(new MotionModuleOptions
        {
            HostRegionName = MotionRegionNames.HostContent,
            WorkspaceRegionName = MotionRegionNames.WorkspaceContent,
            DefaultDriverKey = "Mock"
        });
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<WorkbenchModule>();
        moduleCatalog.AddModule<CommunicationModule>();
        moduleCatalog.AddModule<WorkflowModule>();
        moduleCatalog.AddModule<DiagnosticsModule>();
        moduleCatalog.AddModule<SettingsModule>();
        moduleCatalog.AddModule<MotionModule>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var registry = Container.Resolve<INavigationRegistry>();
        var home = registry.FindItem("workbench.home")
            ?? throw new InvalidOperationException("工作台导航未注册。");
        Container.Resolve<IShellNavigationService>()
            .NavigateAsync(home)
            .GetAwaiter()
            .GetResult();
    }
}
