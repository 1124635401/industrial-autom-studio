using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;

namespace IndustrialAutomationStudio.Modules.Motion.Views;

public partial class PointDebugView : UserControl
{
    private readonly DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(200)
    };

    public PointDebugView()
    {
        InitializeComponent();
        _refreshTimer.Tick += RefreshTimer_OnTick;
    }

    private void PointDebugView_OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _refreshTimer.Start();
    }

    private void PointDebugView_OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _refreshTimer.Stop();
        if (DataContext is PointDebugViewModel viewModel)
        {
            _ = viewModel.StopGroupAsync();
        }
    }

    private void RefreshTimer_OnTick(object? sender, EventArgs e)
    {
        if (DataContext is PointDebugViewModel viewModel)
        {
            viewModel.RefreshPositionsCommand.Execute();
        }
    }

    private void DirectionButton_OnPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not JogDirectionViewModel direction ||
            DataContext is not PointDebugViewModel viewModel)
        {
            return;
        }

        button.CaptureMouse();
        _ = viewModel.StartJogAsync(direction);
        e.Handled = true;
    }

    private void DirectionButton_OnPreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        StopJog(sender);
        if (sender is Button { IsMouseCaptured: true } button)
        {
            button.ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    private void DirectionButton_OnLostMouseCapture(
        object sender,
        MouseEventArgs e) => StopJog(sender);

    private void StopJog(object sender)
    {
        if (sender is Button { DataContext: JogDirectionViewModel direction } &&
            DataContext is PointDebugViewModel viewModel)
        {
            _ = viewModel.StopJogAsync(direction);
        }
    }
}
