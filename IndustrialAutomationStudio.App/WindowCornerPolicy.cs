using System.Windows;

namespace IndustrialAutomationStudio.App;

internal static class WindowCornerPolicy
{
    public static CornerRadius ForState(WindowState state) =>
        state == WindowState.Maximized
            ? new CornerRadius(0)
            : new CornerRadius(8);
}
