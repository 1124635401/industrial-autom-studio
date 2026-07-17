using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class PlaceholderViewModel : BindableBase, INavigationAware
{
    private string _title = "功能预留";

    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public string Message => "该页面功能后续实现。";

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        Title = navigationContext.Parameters.GetValue<string>("title") ?? "功能预留";
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
