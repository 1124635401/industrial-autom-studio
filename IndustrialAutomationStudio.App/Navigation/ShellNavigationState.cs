using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class ShellNavigationState : IShellNavigationState
{
    private static readonly ShellNavigationSnapshot Empty = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        null);

    public ShellNavigationState()
        : this(Empty)
    {
    }

    public ShellNavigationState(ShellNavigationSnapshot initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        Current = initial;
    }

    public ShellNavigationSnapshot Current { get; private set; }

    public event EventHandler? Changed;

    public void Update(ShellNavigationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (Current == snapshot)
        {
            return;
        }

        Current = snapshot;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
