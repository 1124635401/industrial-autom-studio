namespace IndustrialAutomationStudio.Modules.Motion.Navigation;

public static class MotionNavigationDisplayNames
{
    public static string GetTitle(string route) => route switch
    {
        MotionNavigationNames.Home => "运动首页",
        MotionNavigationNames.Connection => "控制卡连接",
        MotionNavigationNames.AxisConfig => "轴配置",
        MotionNavigationNames.AxisDebug => "单轴调试",
        MotionNavigationNames.IoMonitor => "IO 监控",
        MotionNavigationNames.PointDebug => "点位调试",
        MotionNavigationNames.MultiAxis => "多轴运动",
        MotionNavigationNames.Alarm => "报警诊断",
        MotionNavigationNames.Log => "运动日志",
        _ => route
    };
}
