namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record IoPageToken(int? PageNumber, string Label, bool IsSelected)
{
    public bool IsEllipsis => PageNumber is null;
}
