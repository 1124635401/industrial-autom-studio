namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public sealed record NavigationModule
{
    public NavigationModule(
        string key,
        string title,
        string iconKey,
        int order,
        NavigationPlacement placement,
        NavigationModuleDisplayMode displayMode,
        string defaultItemKey,
        bool isVisible,
        IReadOnlyList<NavigationGroup> groups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(iconKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultItemKey);
        ArgumentNullException.ThrowIfNull(groups);

        Key = key;
        Title = title;
        IconKey = iconKey;
        Order = order;
        Placement = placement;
        DisplayMode = displayMode;
        DefaultItemKey = defaultItemKey;
        IsVisible = isVisible;
        Groups = groups;
    }

    public string Key { get; }
    public string Title { get; }
    public string IconKey { get; }
    public int Order { get; }
    public NavigationPlacement Placement { get; }
    public NavigationModuleDisplayMode DisplayMode { get; }
    public string DefaultItemKey { get; }
    public bool IsVisible { get; }
    public IReadOnlyList<NavigationGroup> Groups { get; }
}
