namespace IndustrialAutomationStudio.Shell.Contracts.Navigation;

public sealed record NavigationItem
{
    public NavigationItem(
        string key,
        string title,
        string description,
        string iconKey,
        string navigationUri,
        int order,
        bool isVisible = true,
        bool isDevelopment = false,
        string? permission = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(iconKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(navigationUri);

        Key = key;
        Title = title;
        Description = description;
        IconKey = iconKey;
        NavigationUri = navigationUri;
        Order = order;
        IsVisible = isVisible;
        IsDevelopment = isDevelopment;
        Permission = permission;
    }

    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public string IconKey { get; }
    public string NavigationUri { get; }
    public int Order { get; }
    public bool IsVisible { get; }
    public bool IsDevelopment { get; }
    public string? Permission { get; }
}
