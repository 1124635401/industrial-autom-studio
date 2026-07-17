using System.Windows;
using System.Windows.Input;

namespace IndustrialAutomationStudio.App;

public partial class ShellWindow : Window
{
    public ShellWindow() => InitializeComponent();

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove();
    }

    private void Minimize_OnClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_OnClick(object sender, RoutedEventArgs e) =>
        ToggleWindowState();

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState() =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
