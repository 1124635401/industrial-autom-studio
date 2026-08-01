using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.Jog;

public sealed class JogModuleFactory
{
    public JogModuleBuildResult Build(
        AxisGroupConfig group,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(axes);

        var modules = new List<JogModuleViewModel>();
        var missingAxes = new HashSet<AxisAddress>();
        var members = group.Members;
        var x = members.FirstOrDefault(member => member.Role == AxisRole.X);
        var y = members.FirstOrDefault(member => member.Role == AxisRole.Y);
        var consumed = new HashSet<AxisGroupMember>();

        if (x is not null && y is not null)
        {
            modules.Add(CreatePlanarModule(x, y, axes, missingAxes));
            consumed.Add(x);
            consumed.Add(y);
        }

        foreach (var member in members
                     .Where(member => !consumed.Contains(member))
                     .OrderBy(member => RoleOrder(member.Role))
                     .ThenBy(member => member.Address.CardNo)
                     .ThenBy(member => member.Address.AxisNo))
        {
            modules.Add(CreateModule(member, axes, missingAxes));
        }

        return new JogModuleBuildResult(modules, missingAxes.ToArray());
    }

    private static JogModuleViewModel CreateModule(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) =>
        member.Role switch
        {
            AxisRole.X => CreateLinearHorizontalModule(member, axes, missingAxes),
            AxisRole.Y => CreateLinearVerticalModule(
                member,
                JogModuleRegion.Center,
                axes,
                missingAxes),
            AxisRole.Z or AxisRole.V or AxisRole.W =>
                CreateLinearVerticalModule(
                    member,
                    JogModuleRegion.Linear,
                    axes,
                    missingAxes),
            AxisRole.R or AxisRole.U => CreateRotaryModule(member, axes, missingAxes),
            AxisRole.XY => CreatePlanarModule(member, axes, missingAxes),
            _ => CreateUnassignedModule(member, axes, missingAxes)
        };

    private static JogModuleViewModel CreatePlanarModule(
        AxisGroupMember x,
        AxisGroupMember y,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.Planar,
        JogModuleRegion.Center,
        "平台运动",
        "X / Y",
        [
            Direction(y, 1, "Y+", "↑", axes, missingAxes),
            Direction(x, -1, "X−", "←", axes, missingAxes),
            Direction(x, 1, "X+", "→", axes, missingAxes),
            Direction(y, -1, "Y−", "↓", axes, missingAxes)
        ]);

    private static JogModuleViewModel CreatePlanarModule(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.Planar,
        JogModuleRegion.Center,
        AxisName(member, axes, missingAxes),
        "XY",
        [
            Direction(member, 1, "Y+", "↑", axes, missingAxes),
            Direction(member, -1, "X−", "←", axes, missingAxes),
            Direction(member, 1, "X+", "→", axes, missingAxes),
            Direction(member, -1, "Y−", "↓", axes, missingAxes)
        ]);

    private static JogModuleViewModel CreateLinearHorizontalModule(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.LinearHorizontal,
        JogModuleRegion.Center,
        AxisName(member, axes, missingAxes),
        member.Role.ToString(),
        [
            Direction(member, -1, $"{member.Role}−", "←", axes, missingAxes),
            Direction(member, 1, $"{member.Role}+", "→", axes, missingAxes)
        ]);

    private static JogModuleViewModel CreateLinearVerticalModule(
        AxisGroupMember member,
        JogModuleRegion region,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.LinearVertical,
        region,
        AxisName(member, axes, missingAxes),
        member.Role.ToString(),
        [
            Direction(member, 1, $"{member.Role}+", "↑", axes, missingAxes),
            Direction(member, -1, $"{member.Role}−", "↓", axes, missingAxes)
        ]);

    private static JogModuleViewModel CreateRotaryModule(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.Rotary,
        JogModuleRegion.Rotary,
        AxisName(member, axes, missingAxes),
        member.Role.ToString(),
        [
            Direction(member, 1, $"{member.Role}+", "↻", axes, missingAxes),
            Direction(member, -1, $"{member.Role}−", "↺", axes, missingAxes)
        ]);

    private static JogModuleViewModel CreateUnassignedModule(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        JogModuleKind.LinearHorizontal,
        JogModuleRegion.Auxiliary,
        AxisName(member, axes, missingAxes),
        "未分配",
        [
            Direction(member, -1, "−", "←", axes, missingAxes),
            Direction(member, 1, "+", "→", axes, missingAxes)
        ]);

    private static JogDirectionViewModel Direction(
        AxisGroupMember member,
        int direction,
        string label,
        string symbol,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes) => new(
        member.Address,
        member.Role,
        direction,
        AxisName(member, axes, missingAxes),
        label,
        symbol);

    private static string AxisName(
        AxisGroupMember member,
        IReadOnlyDictionary<AxisAddress, AxisConfig> axes,
        ISet<AxisAddress> missingAxes)
    {
        if (axes.TryGetValue(member.Address, out var axis))
        {
            return axis.AxisName;
        }

        missingAxes.Add(member.Address);
        return $"轴 {member.Address.CardNo}:{member.Address.AxisNo}";
    }

    private static int RoleOrder(AxisRole role) => role switch
    {
        AxisRole.XY => 0,
        AxisRole.X => 1,
        AxisRole.Y => 2,
        AxisRole.Z => 3,
        AxisRole.R => 4,
        AxisRole.U => 5,
        AxisRole.V => 6,
        AxisRole.W => 7,
        _ => 8
    };
}

public sealed record JogModuleBuildResult(
    IReadOnlyList<JogModuleViewModel> Modules,
    IReadOnlyCollection<AxisAddress> MissingAxes);
