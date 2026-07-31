using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

public sealed class PointDebugJogPadViewModel
{
    public PointDebugJogPadViewModel(IEnumerable<JogDirectionViewModel> directions)
    {
        ArgumentNullException.ThrowIfNull(directions);
        var directionList = directions.ToArray();
        var bySlot = directionList
            .GroupBy(direction => (direction.Role, direction.Direction))
            .ToDictionary(group => group.Key, group => group.First());

        XNegative = FindPlanarDirection(directionList, bySlot, AxisRole.X, -1);
        XPositive = FindPlanarDirection(directionList, bySlot, AxisRole.X, 1);
        YNegative = FindPlanarDirection(directionList, bySlot, AxisRole.Y, -1);
        YPositive = FindPlanarDirection(directionList, bySlot, AxisRole.Y, 1);
        ZNegative = Find(bySlot, AxisRole.Z, -1);
        ZPositive = Find(bySlot, AxisRole.Z, 1);
        RNegative = Find(bySlot, AxisRole.R, -1);
        RPositive = Find(bySlot, AxisRole.R, 1);
    }

    public JogDirectionViewModel? XNegative { get; }
    public JogDirectionViewModel? XPositive { get; }
    public JogDirectionViewModel? YNegative { get; }
    public JogDirectionViewModel? YPositive { get; }
    public JogDirectionViewModel? ZNegative { get; }
    public JogDirectionViewModel? ZPositive { get; }
    public JogDirectionViewModel? RNegative { get; }
    public JogDirectionViewModel? RPositive { get; }

    private static JogDirectionViewModel? Find(
        IReadOnlyDictionary<(AxisRole Role, int Direction), JogDirectionViewModel> directions,
        AxisRole role,
        int direction) => directions.GetValueOrDefault((role, direction));

    private static JogDirectionViewModel? FindPlanarDirection(
        IReadOnlyList<JogDirectionViewModel> directionList,
        IReadOnlyDictionary<(AxisRole Role, int Direction), JogDirectionViewModel> directions,
        AxisRole role,
        int direction)
    {
        var directMatch = Find(directions, role, direction);
        if (directMatch is not null)
        {
            return directMatch;
        }

        var axisPrefix = role == AxisRole.X ? "X" : "Y";
        return directionList.FirstOrDefault(candidate =>
            candidate.Role == AxisRole.XY &&
            candidate.Direction == direction &&
            candidate.Label.StartsWith(axisPrefix, StringComparison.OrdinalIgnoreCase));
    }
}
