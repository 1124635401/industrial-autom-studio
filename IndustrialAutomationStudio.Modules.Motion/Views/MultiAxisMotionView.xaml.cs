using System.Windows.Controls;
using System.Windows.Input;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;

namespace IndustrialAutomationStudio.Modules.Motion.Views;

public partial class MultiAxisMotionView : UserControl
{
    public MultiAxisMotionView()
    {
        InitializeComponent();
    }

    private void DirectionButton_OnPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not Button button
            || button.DataContext is not JogDirectionViewModel direction
            || DataContext is not MultiAxisMotionViewModel viewModel)
        {
            return;
        }

        viewModel.BeginPreview(direction);
        button.CaptureMouse();
    }

    private void DirectionButton_OnPreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        EndPreview(sender);
        if (sender is Button { IsMouseCaptured: true } button)
        {
            button.ReleaseMouseCapture();
        }
    }

    private void DirectionButton_OnLostMouseCapture(
        object sender,
        MouseEventArgs e) => EndPreview(sender);

    private void EndPreview(object sender)
    {
        if (sender is Button { DataContext: JogDirectionViewModel direction }
            && DataContext is MultiAxisMotionViewModel viewModel)
        {
            viewModel.EndPreview(direction);
        }
    }
}
