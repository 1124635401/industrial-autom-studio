using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Controls;

public partial class PointDebugJogPad : UserControl
{
    public static readonly DependencyProperty JogPadProperty = DependencyProperty.Register(
        nameof(JogPad),
        typeof(PointDebugJogPadViewModel),
        typeof(PointDebugJogPad));

    public static readonly DependencyProperty StartCommandProperty = DependencyProperty.Register(
        nameof(StartCommand),
        typeof(ICommand),
        typeof(PointDebugJogPad));

    public static readonly DependencyProperty StopCommandProperty = DependencyProperty.Register(
        nameof(StopCommand),
        typeof(ICommand),
        typeof(PointDebugJogPad));

    public static readonly DependencyProperty StopAllCommandProperty = DependencyProperty.Register(
        nameof(StopAllCommand),
        typeof(ICommand),
        typeof(PointDebugJogPad));

    public static readonly DependencyProperty IsInteractionEnabledProperty = DependencyProperty.Register(
        nameof(IsInteractionEnabled),
        typeof(bool),
        typeof(PointDebugJogPad),
        new PropertyMetadata(true));

    public PointDebugJogPad()
    {
        InitializeComponent();
    }

    public PointDebugJogPadViewModel? JogPad
    {
        get => (PointDebugJogPadViewModel?)GetValue(JogPadProperty);
        set => SetValue(JogPadProperty, value);
    }

    public ICommand? StartCommand
    {
        get => (ICommand?)GetValue(StartCommandProperty);
        set => SetValue(StartCommandProperty, value);
    }

    public ICommand? StopCommand
    {
        get => (ICommand?)GetValue(StopCommandProperty);
        set => SetValue(StopCommandProperty, value);
    }

    public ICommand? StopAllCommand
    {
        get => (ICommand?)GetValue(StopAllCommandProperty);
        set => SetValue(StopAllCommandProperty, value);
    }

    public bool IsInteractionEnabled
    {
        get => (bool)GetValue(IsInteractionEnabledProperty);
        set => SetValue(IsInteractionEnabledProperty, value);
    }

    private void DirectionButton_OnPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not Button { DataContext: JogDirectionViewModel direction } button ||
            StartCommand?.CanExecute(direction) != true)
        {
            return;
        }

        button.CaptureMouse();
        StartCommand.Execute(direction);
        e.Handled = true;
    }

    private void DirectionButton_OnPreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        StopDirection(sender);
        e.Handled = true;
    }

    private void DirectionButton_OnLostMouseCapture(object sender, MouseEventArgs e) =>
        StopDirection(sender);

    private void StopDirection(object sender)
    {
        if (sender is not Button { DataContext: JogDirectionViewModel direction } button)
        {
            return;
        }

        if (StopCommand?.CanExecute(direction) == true)
        {
            StopCommand.Execute(direction);
        }

        if (button.IsMouseCaptured)
        {
            button.ReleaseMouseCapture();
        }
    }
}
