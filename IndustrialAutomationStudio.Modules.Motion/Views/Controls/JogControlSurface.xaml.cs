using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Controls;

public partial class JogControlSurface : UserControl
{
    public static readonly DependencyProperty AxisCountProperty =
        DependencyProperty.Register(
            nameof(AxisCount),
            typeof(int),
            typeof(JogControlSurface),
            new PropertyMetadata(0));

    public static readonly DependencyProperty CenterModulesProperty =
        DependencyProperty.Register(
            nameof(CenterModules),
            typeof(IEnumerable),
            typeof(JogControlSurface));

    public static readonly DependencyProperty LinearModulesProperty =
        DependencyProperty.Register(
            nameof(LinearModules),
            typeof(IEnumerable),
            typeof(JogControlSurface));

    public static readonly DependencyProperty RotaryModulesProperty =
        DependencyProperty.Register(
            nameof(RotaryModules),
            typeof(IEnumerable),
            typeof(JogControlSurface));

    public static readonly DependencyProperty AuxiliaryModulesProperty =
        DependencyProperty.Register(
            nameof(AuxiliaryModules),
            typeof(IEnumerable),
            typeof(JogControlSurface));

    public static readonly DependencyProperty IsInteractionEnabledProperty =
        DependencyProperty.Register(
            nameof(IsInteractionEnabled),
            typeof(bool),
            typeof(JogControlSurface),
            new PropertyMetadata(true));

    public static readonly DependencyProperty StartCommandProperty =
        DependencyProperty.Register(
            nameof(StartCommand),
            typeof(ICommand),
            typeof(JogControlSurface));

    public static readonly DependencyProperty StopCommandProperty =
        DependencyProperty.Register(
            nameof(StopCommand),
            typeof(ICommand),
            typeof(JogControlSurface));

    public static readonly DependencyProperty DensityProperty =
        DependencyProperty.Register(
            nameof(Density),
            typeof(JogSurfaceDensity),
            typeof(JogControlSurface),
            new PropertyMetadata(JogSurfaceDensity.Standard));

    private Button? _activeButton;

    public JogControlSurface() => InitializeComponent();

    public int AxisCount
    {
        get => (int)GetValue(AxisCountProperty);
        set => SetValue(AxisCountProperty, value);
    }

    public IEnumerable? CenterModules
    {
        get => (IEnumerable?)GetValue(CenterModulesProperty);
        set => SetValue(CenterModulesProperty, value);
    }

    public IEnumerable? LinearModules
    {
        get => (IEnumerable?)GetValue(LinearModulesProperty);
        set => SetValue(LinearModulesProperty, value);
    }

    public IEnumerable? RotaryModules
    {
        get => (IEnumerable?)GetValue(RotaryModulesProperty);
        set => SetValue(RotaryModulesProperty, value);
    }

    public IEnumerable? AuxiliaryModules
    {
        get => (IEnumerable?)GetValue(AuxiliaryModulesProperty);
        set => SetValue(AuxiliaryModulesProperty, value);
    }

    public bool IsInteractionEnabled
    {
        get => (bool)GetValue(IsInteractionEnabledProperty);
        set => SetValue(IsInteractionEnabledProperty, value);
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

    public JogSurfaceDensity Density
    {
        get => (JogSurfaceDensity)GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    private void DirectionButton_OnPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not JogDirectionViewModel direction ||
            !IsInteractionEnabled ||
            StartCommand?.CanExecute(direction) != true)
        {
            return;
        }

        _activeButton = button;
        StartCommand.Execute(direction);
        button.CaptureMouse();
        e.Handled = true;
    }

    private void DirectionButton_OnPreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is Button button)
        {
            StopDirection(button);
            if (button.IsMouseCaptured)
            {
                button.ReleaseMouseCapture();
            }
        }

        e.Handled = true;
    }

    private void DirectionButton_OnLostMouseCapture(
        object sender,
        MouseEventArgs e)
    {
        if (sender is Button button)
        {
            StopDirection(button);
        }
    }

    private void StopDirection(Button button)
    {
        if (!ReferenceEquals(_activeButton, button) ||
            button.DataContext is not JogDirectionViewModel direction ||
            StopCommand?.CanExecute(direction) != true)
        {
            return;
        }

        _activeButton = null;
        StopCommand.Execute(direction);
    }
}
