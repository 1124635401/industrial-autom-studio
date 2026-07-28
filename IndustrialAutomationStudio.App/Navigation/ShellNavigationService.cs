using IndustrialAutomationStudio.Shell.Contracts.Navigation;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.App.Navigation;

public sealed class ShellNavigationService : IShellNavigationService
{
    private readonly Func<
        string,
        string,
        NavigationParameters,
        CancellationToken,
        Task<(bool Success, string? Error)>> _requestNavigate;
    private readonly IShellNavigationState _state;
    private readonly INavigationRegistry _registry;

    public ShellNavigationService(
        IRegionManager regionManager,
        IShellNavigationState state,
        INavigationRegistry registry)
        : this(
            (regionName, navigationUri, parameters, cancellationToken) =>
                RequestNavigateAsync(
                    regionManager,
                    regionName,
                    navigationUri,
                    parameters,
                    cancellationToken),
            state,
            registry)
    {
    }

    internal ShellNavigationService(
        Func<
            string,
            string,
            NavigationParameters,
            CancellationToken,
            Task<(bool Success, string? Error)>> requestNavigate,
        IShellNavigationState state,
        INavigationRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(requestNavigate);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(registry);

        _requestNavigate = requestNavigate;
        _state = state;
        _registry = registry;
    }

    public async Task<bool> NavigateAsync(
        NavigationItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var module = FindModule(item.Key);
        if (module is null)
        {
            PublishError($"导航页面“{item.Key}”没有所属模块。");
            return false;
        }

        var breadcrumb = $"{module.Title} / {item.Title}";
        var parameters = new NavigationParameters
        {
            { "moduleKey", module.Key },
            { "moduleTitle", module.Title },
            { "itemKey", item.Key },
            { "itemTitle", item.Title },
            { "description", item.Description },
            { "breadcrumb", breadcrumb }
        };

        var (success, error) = await _requestNavigate(
            ShellRegionNames.MainContent,
            item.NavigationUri,
            parameters,
            cancellationToken);

        if (!success)
        {
            PublishError(error ?? $"无法打开页面“{item.Title}”。");
            return false;
        }

        _state.Update(
            new ShellNavigationSnapshot(
                module.Key,
                module.Title,
                item.Key,
                item.Title,
                item.Description,
                breadcrumb,
                null));
        return true;
    }

    private NavigationModule? FindModule(string itemKey) =>
        _registry.Modules.FirstOrDefault(
            module => module.Groups
                .SelectMany(group => group.Items)
                .Any(item => string.Equals(
                    item.Key,
                    itemKey,
                    StringComparison.Ordinal)));

    private void PublishError(string message) =>
        _state.Update(_state.Current with { ErrorMessage = message });

    private static Task<(bool Success, string? Error)> RequestNavigateAsync(
        IRegionManager regionManager,
        string regionName,
        string navigationUri,
        NavigationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(regionManager);

        var completion = new TaskCompletionSource<(bool Success, string? Error)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = cancellationToken.Register(
            () => completion.TrySetCanceled(cancellationToken));

        regionManager.RequestNavigate(
            regionName,
            navigationUri,
            result =>
            {
                registration.Dispose();
                var error = result.Exception?.Message;
                if (result.Cancelled && string.IsNullOrWhiteSpace(error))
                {
                    error = "导航已取消。";
                }

                completion.TrySetResult((result.Success, error));
            },
            parameters);

        return completion.Task;
    }
}
