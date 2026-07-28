using IndustrialAutomationStudio.App.Navigation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IndustrialAutomationStudio.App;

public partial class ShellWindow : Window
{
    private CancellationTokenSource? _hoverCancellation;
    private CancellationTokenSource? _leaveCancellation;

    public ShellWindow()
    {
        InitializeComponent();
        ApplyWindowCorners();
    }

    private ShellWindowViewModel ViewModel =>
        (ShellWindowViewModel)DataContext;

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

    private async void Module_OnMouseEnter(
        object sender,
        MouseEventArgs e)
    {
        if (sender is not FrameworkElement
            {
                DataContext: ShellNavigationModuleViewModel module
            }
            || !module.HasMenu)
        {
            return;
        }

        Cancel(ref _leaveCancellation);
        Cancel(ref _hoverCancellation);
        _hoverCancellation = new CancellationTokenSource();
        try
        {
            await ViewModel.EnterModuleAsync(
                module.Key,
                _hoverCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Module_OnMouseLeave(object sender, MouseEventArgs e)
    {
        Cancel(ref _hoverCancellation);
        ScheduleMenuLeave();
    }

    private void MenuPopup_OnMouseEnter(object sender, MouseEventArgs e) =>
        Cancel(ref _leaveCancellation);

    private void MenuPopup_OnMouseLeave(object sender, MouseEventArgs e) =>
        ScheduleMenuLeave();

    private void Shell_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source
            && FindAncestor<Button>(source) is not null)
        {
            return;
        }

        ViewModel.CloseMenu();
    }

    private void Shell_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        ViewModel.CloseMenu();
        e.Handled = true;
    }

    private void Shell_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var layout = TitleBarLayoutPolicy.ForWidth(e.NewSize.Width);
        ProductName.Text = layout.ProductName;
        SupportHelpAction.Visibility = layout.ShowSupportActions
            ? Visibility.Visible
            : Visibility.Collapsed;
        SupportNotificationAction.Visibility = layout.ShowSupportActions
            ? Visibility.Visible
            : Visibility.Collapsed;
        UserName.Visibility = layout.ShowUserDetails
            ? Visibility.Visible
            : Visibility.Collapsed;
        UserMenuChevron.Visibility = layout.ShowUserDetails
            ? Visibility.Visible
            : Visibility.Collapsed;
        GlobalConnectionStatus.Visibility = layout.ShowConnectionStatus
            ? Visibility.Visible
            : Visibility.Collapsed;
        Resources["ShellTopNavigationPadding"] = layout.NavigationPadding;
        Resources["ShellTopNavigationMargin"] = layout.NavigationMargin;
    }

    private void Shell_OnStateChanged(object? sender, EventArgs e) =>
        ApplyWindowCorners();

    private void ApplyWindowCorners()
    {
        var radius = WindowCornerPolicy.ForState(WindowState);
        WindowFrame.CornerRadius = radius;

        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
        if (chrome is not null)
        {
            chrome.CornerRadius = radius;
        }
    }

    private void ToggleWindowState() =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private async void ScheduleMenuLeave()
    {
        Cancel(ref _leaveCancellation);
        _leaveCancellation = new CancellationTokenSource();
        try
        {
            await ViewModel.LeaveMenuAsync(_leaveCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void Cancel(ref CancellationTokenSource? cancellation)
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }

    private static T? FindAncestor<T>(DependencyObject source)
        where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
