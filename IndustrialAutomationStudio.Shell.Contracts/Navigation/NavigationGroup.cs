namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public sealed record NavigationGroup
{
    public NavigationGroup(
        string key,
        string title,
        int order,
        bool isVisible,
        IReadOnlyList<NavigationItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);

        Key = key;
        Title = title;
        Order = order;
        IsVisible = isVisible;
        Items = items;
    }

    public string Key { get; }
    public string Title { get; }
    public int Order { get; }
    public bool IsVisible { get; }
    public IReadOnlyList<NavigationItem> Items { get; }
}
