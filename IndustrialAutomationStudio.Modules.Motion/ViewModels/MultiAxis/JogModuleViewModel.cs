namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;

public sealed class JogModuleViewModel
{
    public JogModuleViewModel(
        JogModuleKind kind,
        JogModuleRegion region,
        string title,
        string roleLabel,
        IReadOnlyList<JogDirectionViewModel> directions)
    {
        Kind = kind;
        Region = region;
        Title = title;
        RoleLabel = roleLabel;
        Directions = directions;
    }

    public JogModuleKind Kind { get; }
    public JogModuleRegion Region { get; }
    public string Title { get; }
    public string RoleLabel { get; }
    public IReadOnlyList<JogDirectionViewModel> Directions { get; }
}
