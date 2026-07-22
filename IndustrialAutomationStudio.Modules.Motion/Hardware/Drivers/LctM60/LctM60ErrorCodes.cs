namespace IndustrialAutomationStudio.Modules.Motion.Hardware.Drivers.LctM60;

internal static class LctM60ErrorCodes
{
    public static string Describe(short code) => code switch
    {
        1 => "指令执行错误",
        2 => "板卡固件未授权",
        3 => "接口参数错误",
        4 => "设备未打开",
        5 => "EtherCAT 未连接",
        6 => "设备离线",
        7 => "FPGA 操作超时",
        8 => "SDO 返回超时",
        9 => "板卡驱动故障",
        10 => "配置文件打开失败",
        11 => "配置文件操作失败",
        12 => "系统资源不足",
        13 => "ENI 未加载",
        14 => "指令未定义",
        15 => "数据校验错误",
        16 => "数据写入超时",
        17 => "数据读取超时",
        19 => "伺服未使能",
        20 => "从站别名冲突",
        21 => "ENI 中找不到对应从站",
        22 => "看门狗超时",
        23 => "急停信号已触发",
        30 => "EtherCAT 网络拓扑已变化",
        _ => "未知错误"
    };
}
