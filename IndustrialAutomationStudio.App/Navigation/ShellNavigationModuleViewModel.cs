using IndustrialAutomationStudio.Shell.Contracts.Navigation;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class ShellNavigationModuleViewModel : BindableBase
{
    private bool _isActive;
    private bool _isMenuOpen;

    public ShellNavigationModuleViewModel(NavigationModule model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        Groups = model.Groups
            .Select(group => new ShellNavigationGroupViewModel(group))
            .ToArray();
        DefaultItem = Groups
            .SelectMany(group => group.Items)
            .Single(item => string.Equals(
                item.Key,
                model.DefaultItemKey,
                StringComparison.Ordinal));
    }

    public NavigationModule Model { get; }
    public string Key => Model.Key;
    public string Title => Model.Title;
    public string IconKey => Model.IconKey;
    public NavigationModuleDisplayMode DisplayMode => Model.DisplayMode;
    public bool HasMenu => DisplayMode != NavigationModuleDisplayMode.Direct;
    public IReadOnlyList<ShellNavigationGroupViewModel> Groups { get; }
    public ShellNavigationItemViewModel DefaultItem { get; }

    public bool IsActive
    {
        get => _isActive;
        internal set => SetProperty(ref _isActive, value);
    }

    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        internal set => SetProperty(ref _isMenuOpen, value);
    }
}
