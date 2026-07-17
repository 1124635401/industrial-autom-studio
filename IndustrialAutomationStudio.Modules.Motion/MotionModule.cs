using IndustrialAutomationStudio.Modules.Motion.Hardware;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.Mock;
using IndustrialAutomationStudio.Modules.Motion.Hardware.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Navigation;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Json;
using IndustrialAutomationStudio.Modules.Motion.Services.Implementations;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.Dialogs;
using IndustrialAutomationStudio.Modules.Motion.Views;
using IndustrialAutomationStudio.Modules.Motion.Views.Dialogs;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;

namespace IndustrialAutomationStudio.Modules.Motion;

public sealed class MotionModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IMotionCardDriverFactory, MockMotionCardDriverFactory>();
        containerRegistry.RegisterSingleton<DriverRegistry>();
        containerRegistry.RegisterSingleton<IAxisConfigValidator, AxisConfigValidator>();
        containerRegistry.RegisterSingleton<IAxisConfigurationValidator, AxisConfigurationValidator>();
        containerRegistry.RegisterSingleton<IAxisConfigRepository, JsonAxisConfigRepository>();
        containerRegistry.RegisterSingleton<IMotionCardConfigRepository, JsonMotionCardConfigRepository>();
        containerRegistry.RegisterSingleton<IMotionLogService, InMemoryMotionLogService>();
        containerRegistry.RegisterSingleton<IMotionCardService, MotionCardService>();
        containerRegistry.RegisterSingleton<IAxisConfigService, AxisConfigService>();
        containerRegistry.RegisterSingleton<IMotionConfigurationService, MotionConfigurationService>();
        containerRegistry.RegisterSingleton<IConfigurationFileDialogService, WpfConfigurationFileDialogService>();
        containerRegistry.RegisterSingleton<IConfigurationPromptService, WpfConfigurationPromptService>();
        containerRegistry.RegisterSingleton<IMotionSafetyService, MotionSafetyService>();
        containerRegistry.Register<AddAxisDialogViewModel>();
        containerRegistry.Register<AddAxisDialog>();

        containerRegistry.RegisterForNavigation<MotionWorkspaceView, MotionWorkspaceViewModel>(
            MotionNavigationNames.Workspace);
        containerRegistry.RegisterForNavigation<MotionHomeView, MotionHomeViewModel>(
            MotionNavigationNames.Home);
        containerRegistry.RegisterForNavigation<MotionConnectionView, MotionConnectionViewModel>(
            MotionNavigationNames.Connection);
        containerRegistry.RegisterForNavigation<AxisConfigView, AxisConfigViewModel>(
            MotionNavigationNames.AxisConfig);
        containerRegistry.RegisterForNavigation<MotionLogView, MotionLogViewModel>(
            MotionNavigationNames.Log);
        containerRegistry.RegisterForNavigation<PlaceholderView, PlaceholderViewModel>(
            MotionNavigationNames.AxisDebug);
        containerRegistry.RegisterForNavigation<PlaceholderView, PlaceholderViewModel>(
            MotionNavigationNames.IoMonitor);
        containerRegistry.RegisterForNavigation<PlaceholderView, PlaceholderViewModel>(
            MotionNavigationNames.PointDebug);
        containerRegistry.RegisterForNavigation<PlaceholderView, PlaceholderViewModel>(
            MotionNavigationNames.MultiAxis);
        containerRegistry.RegisterForNavigation<PlaceholderView, PlaceholderViewModel>(
            MotionNavigationNames.Alarm);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var resources = Application.Current?.Resources.MergedDictionaries;
        if (resources is null)
        {
            return;
        }

        var source = new Uri(
            "/IndustrialAutomationStudio.Modules.Motion;component/Resources/MotionTheme.xaml",
            UriKind.RelativeOrAbsolute);
        if (resources.All(dictionary => dictionary.Source != source))
        {
            resources.Add(new ResourceDictionary { Source = source });
        }
    }
}
