using IndustrialAutomationStudio.Shell.Contracts.Navigation;

namespace IndustrialAutomationStudio.Modules.Motion.Navigation;

public sealed class MotionNavigationCatalog : INavigationContributor
{
    public NavigationModule CreateNavigationModule()
    {
        var configuration = new NavigationGroup(
            "motion.configuration",
            "运控配置",
            0,
            true,
            [
                Item(
                    "motion.connection",
                    "控制卡连接",
                    "配置、测试并管理运动控制卡连接",
                    "MotionIcon.Connection",
                    MotionNavigationNames.Connection,
                    0),
                Item(
                    "motion.axis-config",
                    "轴配置",
                    "维护轴参数、单位换算、限位与原点配置",
                    "MotionIcon.Axis",
                    MotionNavigationNames.AxisConfig,
                    10),
                Item(
                    "motion.group-management",
                    "分组管理",
                    "配置轴组成员、顺序和插补参数",
                    "MotionIcon.Group",
                    MotionNavigationNames.GroupManagement,
                    20),
                Item(
                    "motion.io-monitor",
                    "IO 监控",
                    "查看并调试数字输入和输出点位",
                    "MotionIcon.Io",
                    MotionNavigationNames.IoMonitor,
                    30)
            ]);

        var debug = new NavigationGroup(
            "motion.debug",
            "运动调试",
            10,
            true,
            [
                Item(
                    "motion.point-debug",
                    "点位调试",
                    "读取、维护并移动到设备点位",
                    "MotionIcon.Point",
                    MotionNavigationNames.PointDebug,
                    0),
                Item(
                    "motion.multi-axis",
                    "多轴运动",
                    "执行多轴点动与同步定位",
                    "MotionIcon.MultiAxis",
                    MotionNavigationNames.MultiAxis,
                    10)
            ]);

        return new NavigationModule(
            "motion",
            "运控调试",
            "MotionIcon.Motion",
            10,
            NavigationPlacement.Primary,
            NavigationModuleDisplayMode.Menu,
            "motion.connection",
            true,
            [configuration, debug]);
    }

    private static NavigationItem Item(
        string key,
        string title,
        string description,
        string iconKey,
        string navigationUri,
        int order) =>
        new(
            key,
            title,
            description,
            iconKey,
            navigationUri,
            order);
}
