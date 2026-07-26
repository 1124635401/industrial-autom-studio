using System.Windows;
using System.Windows.Controls;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Controls;

public sealed class AdaptiveMotionConsole : Decorator
{
    public static readonly DependencyProperty AxisCountProperty =
        DependencyProperty.Register(
            nameof(AxisCount),
            typeof(int),
            typeof(AdaptiveMotionConsole),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int AxisCount
    {
        get => (int)GetValue(AxisCountProperty);
        set => SetValue(AxisCountProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child is null)
        {
            return Size.Empty;
        }

        var availableWidth =
            MultiAxisConsoleSizing.NormalizeAvailableWidth(constraint.Width);
        var maximumWidth = MultiAxisConsoleSizing.MaximumWidth(
            AxisCount,
            availableWidth);

        Child.Measure(new Size(maximumWidth, constraint.Height));

        var width = MultiAxisConsoleSizing.Calculate(
            AxisCount,
            availableWidth,
            Child.DesiredSize.Width,
            0);
        return new Size(width, Child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(new Point(), arrangeSize));
        return arrangeSize;
    }
}
