namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public interface INavigationRegistry
{
    IReadOnlyList<NavigationModule> Modules { get; }
    void Register(NavigationModule module);
    NavigationItem? FindItem(string itemKey);
}
