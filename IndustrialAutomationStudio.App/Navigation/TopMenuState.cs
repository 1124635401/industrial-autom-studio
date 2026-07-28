namespace IndustrialAutomationStudio.App.Navigation;

public sealed class TopMenuState
{
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan LeaveDelay = TimeSpan.FromMilliseconds(300);
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public TopMenuState()
        : this(Task.Delay)
    {
    }

    internal TopMenuState(Func<TimeSpan, CancellationToken, Task> delay)
    {
        ArgumentNullException.ThrowIfNull(delay);
        _delay = delay;
    }

    public string? OpenModuleKey { get; private set; }
    public string? PinnedModuleKey { get; private set; }

    public event EventHandler? Changed;

    public async Task EnterAsync(
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        if (string.Equals(OpenModuleKey, moduleKey, StringComparison.Ordinal))
        {
            return;
        }

        await _delay(HoverDelay, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        SetState(moduleKey, PinnedModuleKey);
    }

    public void Pin(string moduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        SetState(moduleKey, moduleKey);
    }

    public void Unpin() => SetState(OpenModuleKey, null);

    public async Task LeaveAsync(CancellationToken cancellationToken = default)
    {
        if (PinnedModuleKey is not null || OpenModuleKey is null)
        {
            return;
        }

        await _delay(LeaveDelay, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (PinnedModuleKey is null)
        {
            SetState(null, null);
        }
    }

    public void Close() => SetState(null, null);

    private void SetState(string? openModuleKey, string? pinnedModuleKey)
    {
        if (string.Equals(OpenModuleKey, openModuleKey, StringComparison.Ordinal)
            && string.Equals(PinnedModuleKey, pinnedModuleKey, StringComparison.Ordinal))
        {
            return;
        }

        OpenModuleKey = openModuleKey;
        PinnedModuleKey = pinnedModuleKey;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
