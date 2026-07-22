using System.Windows;
using System.Windows.Controls;

namespace IndustrialAutomationStudio.Modules.Motion.Views;

public partial class MotionConnectionView : UserControl
{
    private const double CompactStatusThreshold = 980;
    private bool? _usesCompactStatusLayout;

    public MotionConnectionView() => InitializeComponent();

    private void MotionConnectionView_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var usesCompactLayout = e.NewSize.Width < CompactStatusThreshold;
        if (_usesCompactStatusLayout == usesCompactLayout)
        {
            return;
        }

        _usesCompactStatusLayout = usesCompactLayout;
        StatusIdentityColumn.Width = new GridLength(usesCompactLayout ? 1 : 3, GridUnitType.Star);
        StatusLeftGapColumn.Width = new GridLength(usesCompactLayout ? 0 : 24);
        StatusMetricsColumn.Width = new GridLength(usesCompactLayout ? 0 : 4, GridUnitType.Star);
        StatusMiddleGapColumn.Width = new GridLength(usesCompactLayout ? 0 : 24);
        StatusDetailsColumn.Width = new GridLength(usesCompactLayout ? 0 : 3, GridUnitType.Star);
        StatusRightGapColumn.Width = new GridLength(usesCompactLayout ? 0 : 24);
        StatusResultColumn.Width = new GridLength(usesCompactLayout ? 0 : 4, GridUnitType.Star);

        SetStatusPanelPosition(StatusIdentityPanel, 0, 0, new Thickness(0));
        SetStatusPanelPosition(
            StatusMetricsPanel,
            usesCompactLayout ? 1 : 0,
            usesCompactLayout ? 0 : 2,
            usesCompactLayout ? new Thickness(0, 18, 0, 0) : new Thickness(0));
        SetStatusPanelPosition(
            StatusDetailsPanel,
            usesCompactLayout ? 2 : 0,
            usesCompactLayout ? 0 : 4,
            usesCompactLayout ? new Thickness(0, 18, 0, 0) : new Thickness(0));
        SetStatusPanelPosition(
            StatusResultPanel,
            usesCompactLayout ? 3 : 0,
            usesCompactLayout ? 0 : 6,
            usesCompactLayout ? new Thickness(0, 18, 0, 0) : new Thickness(0));

        var separatorVisibility = usesCompactLayout ? Visibility.Collapsed : Visibility.Visible;
        StatusLeftSeparator.Visibility = separatorVisibility;
        StatusMiddleSeparator.Visibility = separatorVisibility;
        StatusRightSeparator.Visibility = separatorVisibility;
    }

    private static void SetStatusPanelPosition(
        FrameworkElement panel,
        int row,
        int column,
        Thickness margin)
    {
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
        panel.Margin = margin;
    }
}
