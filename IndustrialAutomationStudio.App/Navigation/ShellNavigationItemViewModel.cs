using IndustrialAutomationStudio.Shell.Contracts.Navigation;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class ShellNavigationItemViewModel : BindableBase
{
    private bool _isActive;

    public ShellNavigationItemViewModel(NavigationItem model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public NavigationItem Model { get; }
    public string Key => Model.Key;
    public string Title => Model.Title;
    public string Description => Model.Description;
    public string IconKey => Model.IconKey;
    public bool IsDevelopment => Model.IsDevelopment;

    public bool IsActive
    {
        get => _isActive;
        internal set => SetProperty(ref _isActive, value);
    }
}
