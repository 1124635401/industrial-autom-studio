using System.Windows;
using System.Windows.Controls;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Controls;

public sealed class MultiAxisJogLayoutPanel : Panel
{
    private const double RegionGap = 16;
    private const double MaximumCenterWidth = 410;
    private const double MaximumSideWidth = 420;

    public static readonly DependencyProperty AxisCountProperty =
        DependencyProperty.Register(
            nameof(AxisCount),
            typeof(int),
            typeof(MultiAxisJogLayoutPanel),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int AxisCount
    {
        get => (int)GetValue(AxisCountProperty);
        set => SetValue(AxisCountProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = NormalizeWidth(availableSize.Width);
        var measurementWidth = MultiAxisConsoleSizing.MaximumWidth(
            AxisCount,
            availableWidth);
        var centerWidth = Math.Min(MaximumCenterWidth, measurementWidth);
        var sideWidth = Math.Min(
            MaximumSideWidth,
            Math.Max(0, (measurementWidth - RegionGap) / 2));

        MeasureChild(0, sideWidth);
        MeasureChild(1, centerWidth);
        MeasureChild(2, sideWidth);

        var linear = ChildDesiredSize(0);
        var center = ChildDesiredSize(1);
        var rotary = ChildDesiredSize(2);
        var visibleRegionCount =
            (HasContent(linear) ? 1 : 0)
            + (HasContent(center) ? 1 : 0)
            + (HasContent(rotary) ? 1 : 0);
        var contentWidth =
            (HasContent(linear) ? linear.Width : 0)
            + (HasContent(center) ? center.Width : 0)
            + (HasContent(rotary) ? rotary.Width : 0)
            + Math.Max(0, visibleRegionCount - 1) * RegionGap;
        var preferredWidth = MultiAxisConsoleSizing.Calculate(
            AxisCount,
            availableWidth,
            contentWidth,
            visibleRegionCount);

        return MultiAxisJogLayout.Calculate(
                new Size(preferredWidth, availableSize.Height),
                linear,
                center,
                rotary,
                RegionGap)
            .DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = MultiAxisJogLayout.Calculate(
            finalSize,
            ChildDesiredSize(0),
            ChildDesiredSize(1),
            ChildDesiredSize(2),
            RegionGap);

        ArrangeChild(0, layout.Linear);
        ArrangeChild(1, layout.Center);
        ArrangeChild(2, layout.Rotary);
        return finalSize;
    }

    private void MeasureChild(int index, double width)
    {
        if (index < InternalChildren.Count)
        {
            InternalChildren[index].Measure(
                new Size(width, double.PositiveInfinity));
        }
    }

    private Size ChildDesiredSize(int index) =>
        index < InternalChildren.Count
            ? InternalChildren[index].DesiredSize
            : Size.Empty;

    private void ArrangeChild(int index, Rect bounds)
    {
        if (index < InternalChildren.Count)
        {
            InternalChildren[index].Arrange(bounds);
        }
    }

    private static double NormalizeWidth(double width) =>
        MultiAxisConsoleSizing.NormalizeAvailableWidth(width);

    private static bool HasContent(Size size) =>
        size.Width > 0 && size.Height > 0;
}

internal static class MultiAxisConsoleSizing
{
    private const double RegionBreathingRoom = 96;
    private const double FallbackAvailableWidth = 1560;

    internal static double Calculate(
        int axisCount,
        double availableWidth,
        double contentWidth,
        int visibleRegionCount)
    {
        var available = NormalizeAvailableWidth(availableWidth);
        if (available <= 0)
        {
            return 0;
        }

        var (minimum, maximum) = WidthBand(axisCount);
        var contentPreferred =
            NormalizeContentWidth(contentWidth)
            + Math.Max(0, visibleRegionCount) * RegionBreathingRoom;
        var bandPreferred = Math.Clamp(contentPreferred, minimum, maximum);
        return Math.Min(available, bandPreferred);
    }

    internal static double MaximumWidth(int axisCount, double availableWidth)
    {
        var available = NormalizeAvailableWidth(availableWidth);
        var (_, maximum) = WidthBand(axisCount);
        return Math.Min(available, maximum);
    }

    private static (double Minimum, double Maximum) WidthBand(int axisCount) =>
        axisCount switch
        {
            <= 2 => (480, 720),
            <= 5 => (860, 1120),
            <= 8 => (1120, 1380),
            _ => (1280, 1560)
        };

    internal static double NormalizeAvailableWidth(double value) =>
        double.IsFinite(value)
            ? Math.Max(0, value)
            : FallbackAvailableWidth;

    private static double NormalizeContentWidth(double value) =>
        double.IsFinite(value) ? Math.Max(0, value) : 0;
}

internal static class MultiAxisJogLayout
{
    internal static MultiAxisJogLayoutResult Calculate(
        Size available,
        Size linear,
        Size center,
        Size rotary,
        double gap)
    {
        var width = Math.Max(
            0,
            double.IsFinite(available.Width) ? available.Width : 0);
        var linearVisible = IsVisible(linear);
        var centerVisible = IsVisible(center);
        var rotaryVisible = IsVisible(rotary);
        var visibleCount =
            (linearVisible ? 1 : 0)
            + (centerVisible ? 1 : 0)
            + (rotaryVisible ? 1 : 0);
        var singleRowWidth =
            (linearVisible ? linear.Width : 0)
            + (centerVisible ? center.Width : 0)
            + (rotaryVisible ? rotary.Width : 0)
            + Math.Max(0, visibleCount - 1) * gap;

        if (singleRowWidth <= width || visibleCount <= 1)
        {
            return SingleRow(
                width,
                linear,
                center,
                rotary,
                gap,
                singleRowWidth);
        }

        if (centerVisible)
        {
            return CenterFirstRows(width, linear, center, rotary, gap);
        }

        return StackedSides(width, linear, rotary, gap);
    }

    private static MultiAxisJogLayoutResult SingleRow(
        double availableWidth,
        Size linear,
        Size center,
        Size rotary,
        double gap,
        double rowWidth)
    {
        var visibleCount =
            (IsVisible(linear) ? 1 : 0)
            + (IsVisible(center) ? 1 : 0)
            + (IsVisible(rotary) ? 1 : 0);
        var expansion = visibleCount == 0
            ? 0
            : Math.Max(0, availableWidth - rowWidth) / visibleCount;
        var height = Math.Max(linear.Height, Math.Max(center.Height, rotary.Height));
        var x = 0d;
        var linearBounds = NextExpandedBounds(
            ref x,
            linear,
            expansion,
            gap,
            height);
        var centerBounds = NextExpandedBounds(
            ref x,
            center,
            expansion,
            gap,
            height);
        var rotaryBounds = NextExpandedBounds(
            ref x,
            rotary,
            expansion,
            gap,
            height);
        return new MultiAxisJogLayoutResult(
            linearBounds,
            centerBounds,
            rotaryBounds,
            new Size(availableWidth, height));
    }

    private static MultiAxisJogLayoutResult CenterFirstRows(
        double availableWidth,
        Size linear,
        Size center,
        Size rotary,
        double gap)
    {
        var centerBounds = Centered(availableWidth, 0, center);
        var sideCount = (IsVisible(linear) ? 1 : 0) + (IsVisible(rotary) ? 1 : 0);
        var sideWidth =
            (IsVisible(linear) ? linear.Width : 0)
            + (IsVisible(rotary) ? rotary.Width : 0)
            + Math.Max(0, sideCount - 1) * gap;
        var sideY = center.Height + gap;
        var sideX = Math.Max(0, (availableWidth - sideWidth) / 2);
        var linearBounds = NextBounds(ref sideX, linear, gap, sideY);
        var rotaryBounds = NextBounds(ref sideX, rotary, gap, sideY);
        var sideHeight = Math.Max(linear.Height, rotary.Height);
        return new MultiAxisJogLayoutResult(
            linearBounds,
            centerBounds,
            rotaryBounds,
            new Size(
                Math.Min(availableWidth, Math.Max(center.Width, sideWidth)),
                center.Height + gap + sideHeight));
    }

    private static MultiAxisJogLayoutResult StackedSides(
        double availableWidth,
        Size linear,
        Size rotary,
        double gap)
    {
        var linearBounds = Centered(availableWidth, 0, linear);
        var rotaryY = linear.Height + (IsVisible(linear) && IsVisible(rotary) ? gap : 0);
        var rotaryBounds = Centered(availableWidth, rotaryY, rotary);
        return new MultiAxisJogLayoutResult(
            linearBounds,
            Rect.Empty,
            rotaryBounds,
            new Size(
                Math.Min(availableWidth, Math.Max(linear.Width, rotary.Width)),
                rotaryY + rotary.Height));
    }

    private static Rect NextBounds(
        ref double x,
        Size size,
        double gap,
        double y = 0)
    {
        if (!IsVisible(size))
        {
            return Rect.Empty;
        }

        var bounds = new Rect(new Point(x, y), size);
        x += size.Width + gap;
        return bounds;
    }

    private static Rect NextExpandedBounds(
        ref double x,
        Size size,
        double expansion,
        double gap,
        double height)
    {
        if (!IsVisible(size))
        {
            return Rect.Empty;
        }

        var bounds = new Rect(
            x,
            0,
            size.Width + expansion,
            height);
        x += bounds.Width + gap;
        return bounds;
    }

    private static Rect Centered(double availableWidth, double y, Size size) =>
        IsVisible(size)
            ? new Rect(
                new Point(Math.Max(0, (availableWidth - size.Width) / 2), y),
                size)
            : Rect.Empty;

    private static bool IsVisible(Size size) =>
        size.Width > 0 && size.Height > 0;
}

internal readonly record struct MultiAxisJogLayoutResult(
    Rect Linear,
    Rect Center,
    Rect Rotary,
    Size DesiredSize);
