using System.Windows;
using System.Windows.Media;

namespace IndustrialAutomationStudio.Modules.Workbench.ViewModels;

public sealed record ResourceMetricViewModel(
    string Name,
    double Value,
    Brush StrokeBrush)
{
    public double Circumference { get; } = 2 * Math.PI * 46;

    public double DashValue => Circumference * Value / 100.0;

    public string DashArray => $"{DashValue:F2} {Circumference:F2}";

    public string ValueText => $"{Value:F0}%";
}

public sealed record SystemResourceSectionViewModel(
    string Key,
    string Title,
    ResourceMetricViewModel Cpu,
    ResourceMetricViewModel Memory,
    ResourceMetricViewModel Disk,
    IReadOnlyList<Point> CpuHistory) : WorkbenchSectionViewModel(Key, Title)
{
    public IReadOnlyList<Point> CpuHistoryArea
    {
        get
        {
            var points = new List<Point>(CpuHistory)
            {
                new Point(320, 100),
                new Point(0, 100)
            };
            return points;
        }
    }
}
