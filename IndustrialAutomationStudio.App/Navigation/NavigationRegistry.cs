using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class NavigationRegistry : INavigationRegistry
{
    private readonly Dictionary<string, NavigationModule> _registeredModules =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _itemOwners =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, NavigationItem> _visibleItems =
        new(StringComparer.Ordinal);

    public IReadOnlyList<NavigationModule> Modules { get; private set; } = [];

    public void Register(NavigationModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_registeredModules.ContainsKey(module.Key))
        {
            throw new InvalidOperationException(
                $"导航模块 Key“{module.Key}”已由其他模块注册。");
        }

        var groups = module.Groups.ToArray();
        var duplicateGroup = groups
            .GroupBy(group => group.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGroup is not null)
        {
            throw new InvalidOperationException(
                $"导航模块“{module.Key}”包含重复的分组 Key“{duplicateGroup.Key}”。");
        }

        var items = groups.SelectMany(group => group.Items).ToArray();
        var duplicateItem = items
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateItem is not null)
        {
            throw new InvalidOperationException(
                $"导航模块“{module.Key}”包含重复的页面 Key“{duplicateItem.Key}”。");
        }

        foreach (var item in items)
        {
            if (_itemOwners.TryGetValue(item.Key, out var owner))
            {
                throw new InvalidOperationException(
                    $"导航页面 Key“{item.Key}”已由模块“{owner}”注册，不能再次由“{module.Key}”注册。");
            }
        }

        if (items.All(item => !string.Equals(
                item.Key,
                module.DefaultItemKey,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"导航模块“{module.Key}”的默认页面“{module.DefaultItemKey}”不存在。");
        }

        var visibleModule = CreateVisibleModule(module);

        _registeredModules.Add(module.Key, visibleModule);
        foreach (var item in items)
        {
            _itemOwners.Add(item.Key, module.Key);
        }

        foreach (var item in visibleModule.Groups.SelectMany(group => group.Items))
        {
            _visibleItems.Add(item.Key, item);
        }

        Modules = _registeredModules.Values
            .Where(registered => registered.IsVisible)
            .OrderBy(registered => registered.Order)
            .ThenBy(registered => registered.Title, StringComparer.Ordinal)
            .ToArray();
    }

    public NavigationItem? FindItem(string itemKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);
        return _visibleItems.GetValueOrDefault(itemKey);
    }

    private static NavigationModule CreateVisibleModule(NavigationModule module)
    {
        var visibleGroups = module.Groups
            .Where(group => group.IsVisible)
            .Select(group => new NavigationGroup(
                group.Key,
                group.Title,
                group.Order,
                true,
                group.Items
                    .Where(item => item.IsVisible)
                    .OrderBy(item => item.Order)
                    .ThenBy(item => item.Title, StringComparer.Ordinal)
                    .ToArray()))
            .Where(group => group.Items.Count > 0)
            .OrderBy(group => group.Order)
            .ThenBy(group => group.Title, StringComparer.Ordinal)
            .ToArray();

        return new NavigationModule(
            module.Key,
            module.Title,
            module.IconKey,
            module.Order,
            module.Placement,
            module.DisplayMode,
            module.DefaultItemKey,
            module.IsVisible,
            visibleGroups);
    }
}
