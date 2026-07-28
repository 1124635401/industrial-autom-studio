using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MotionWorkspaceNavigationItemViewModel : BindableBase
{
    private bool _isActive;

    public MotionWorkspaceNavigationItemViewModel(
        string route,
        string title,
        string iconKey,
        DelegateCommand command,
        bool isActive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);
        ArgumentNullException.ThrowIfNull(command);

        Route = route;
        Title = title;
        IconKey = iconKey;
        Command = command;
        _isActive = isActive;
    }

    public string Route { get; }
    public string Title { get; }
    public string IconKey { get; }
    public DelegateCommand Command { get; }

    public bool IsActive
    {
        get => _isActive;
        internal set => SetProperty(ref _isActive, value);
    }
}
