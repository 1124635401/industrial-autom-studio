namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public interface IShellNavigationService
{
    Task<bool> NavigateAsync(
        NavigationItem item,
        CancellationToken cancellationToken = default);
}
