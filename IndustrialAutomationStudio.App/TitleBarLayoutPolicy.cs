using System.Windows;

namespace IndustrialAutomationStudio.App;

internal readonly record struct TitleBarLayoutState(
    string ProductName,
    bool ShowSupportActions,
    bool ShowUserDetails,
    bool ShowConnectionStatus,
    Thickness NavigationPadding,
    Thickness NavigationMargin);

internal static class TitleBarLayoutPolicy
{
    public static TitleBarLayoutState ForWidth(double width)
    {
        var compact = width < 1240;
        return new TitleBarLayoutState(
            compact ? "调试平台" : "自动化调试平台",
            !compact,
            !compact,
            width >= 1160,
            compact
                ? new Thickness(8, 0, 8, 0)
                : new Thickness(16, 0, 16, 0),
            compact
                ? new Thickness(0, 0, 4, 0)
                : new Thickness(0, 0, 8, 0));
    }
}
