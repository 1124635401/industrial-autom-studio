namespace IndustrialAutomationStudio.Modules.Motion.Models;

public sealed record AxisConfig
{
    public AxisAddress Address { get; init; }
    public string AxisName { get; init; } = string.Empty;
    public string AxisType { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public double GearRatio { get; init; }
    public double Resolution { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int JogReverse { get; init; }
    public double MaxVelocity { get; init; }
    public double Acceleration { get; init; }
    public double Deceleration { get; init; }
    public double STime { get; init; }
    public string HomeMode { get; init; } = string.Empty;
    public string HomeDirection { get; init; } = string.Empty;
    public double HomeAcceleration { get; init; }
    public double HomeVelocity1 { get; init; }
    public double HomeVelocity2 { get; init; }
    public double HomeTimeout { get; init; }
    public double HomeOffset { get; init; }
    public double InPositionError { get; init; }
    public double InPositionTimeout { get; init; }
    public double StopVelocityThreshold { get; init; }
    public int TxPdoStart { get; init; }
    public int RxPdoStart { get; init; }
    public bool IsConfigured { get; init; }

    public static AxisConfig CreateDefault(AxisAddress address, string name) => new()
    {
        Address = address,
        AxisName = name,
        AxisType = "DS402",
        IsEnabled = true,
        GearRatio = 1,
        Resolution = 1000,
        Unit = "mm",
        JogReverse = 1,
        MaxVelocity = 100,
        Acceleration = 1000,
        Deceleration = 1000,
        HomeMode = "ORG_P",
        HomeDirection = "Positive",
        HomeAcceleration = 3000,
        HomeVelocity1 = 20,
        HomeVelocity2 = 5,
        HomeTimeout = 30,
        InPositionError = 0.01,
        InPositionTimeout = 5,
        IsConfigured = true
    };
}
