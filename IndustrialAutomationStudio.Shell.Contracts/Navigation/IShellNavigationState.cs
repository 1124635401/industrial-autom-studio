namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public interface IShellNavigationState
{
    ShellNavigationSnapshot Current { get; }
    event EventHandler? Changed;
    void Update(ShellNavigationSnapshot snapshot);
}
