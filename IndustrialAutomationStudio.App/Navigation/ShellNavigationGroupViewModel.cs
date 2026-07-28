using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class ShellNavigationGroupViewModel
{
    public ShellNavigationGroupViewModel(NavigationGroup model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Key = model.Key;
        Title = model.Title;
        Items = model.Items
            .Select(item => new ShellNavigationItemViewModel(item))
            .ToArray();
    }

    public string Key { get; }
    public string Title { get; }
    public IReadOnlyList<ShellNavigationItemViewModel> Items { get; }
}
