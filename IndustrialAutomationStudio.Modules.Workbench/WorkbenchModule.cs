using IndustrialAutomationStudio.Modules.Workbench.Navigation;
using IndustrialAutomationStudio.Modules.Workbench.ViewModels;
using IndustrialAutomationStudio.Modules.Workbench.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace IndustrialAutomationStudio.Modules.Workbench;

public sealed class WorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry) =>
        containerRegistry.RegisterForNavigation<WorkbenchView, WorkbenchViewModel>(
            WorkbenchNavigationNames.Home);

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
