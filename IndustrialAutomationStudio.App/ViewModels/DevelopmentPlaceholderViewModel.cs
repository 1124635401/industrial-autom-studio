using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.App.ViewModels;

public sealed class DevelopmentPlaceholderViewModel : BindableBase, INavigationAware
{
    private string _moduleTitle = "平台模块";
    private string _pageTitle = "功能页面";
    private string _description = "该功能将在后续阶段提供。";

    public string ModuleTitle
    {
        get => _moduleTitle;
        private set => SetProperty(ref _moduleTitle, value);
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public string StatusText => "开发中/未接入";

    public void OnNavigatedTo(NavigationContext navigationContext) =>
        ApplyParameters(navigationContext.Parameters);

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    internal void ApplyParameters(INavigationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ModuleTitle =
            parameters.GetValue<string>("moduleTitle") ?? "平台模块";
        PageTitle =
            parameters.GetValue<string>("itemTitle") ?? "功能页面";
        Description =
            parameters.GetValue<string>("description") ?? "该功能将在后续阶段提供。";
    }
}
