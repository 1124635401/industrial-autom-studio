using System.Windows.Controls;
using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;

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
}
