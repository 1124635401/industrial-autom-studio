using System.Collections.Generic;

namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record MotionCardConfig
{
    public int CardNo { get; init; }
    public string DriverKey { get; init; } = "Mock";
    public string CardName { get; init; } = "Mock Motion Card";
    public string CardType { get; init; } = "Virtual";
    public string ConnectType { get; init; } = "Mock";
    public string IpAddress { get; init; } = "127.0.0.1";
    public int Port { get; init; }
    public int AxisCount { get; init; } = 8;
    public int DiCount { get; init; } = 64;
    public int DoCount { get; init; } = 64;
    public bool AutoConnectOnStartup { get; init; }
    public string EniFilePath { get; init; } = string.Empty;
    public string SlaveParameterFilePath { get; init; } = string.Empty;
    public int EmergencyInputInverted { get; init; }
    public int EmergencyAction { get; init; }
    public IReadOnlyDictionary<string, string> ConnectionParameters { get; init; }
        = new Dictionary<string, string>();
}
