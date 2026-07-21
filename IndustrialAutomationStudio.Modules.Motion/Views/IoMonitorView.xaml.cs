using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;

namespace IndustrialAutomationStudio.Modules.Motion.Views;

public partial class IoMonitorView : UserControl
{
    public IoMonitorView() => InitializeComponent();

    private void NameText_OnMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ClickCount != 2 ||
            sender is not TextBlock { DataContext: IoPointItem point, Parent: Grid editorHost } ||
            DataContext is not IoMonitorViewModel viewModel)
        {
            return;
        }

        viewModel.BeginEditNameCommand.Execute(point);
        var editor = editorHost.Children.OfType<TextBox>().FirstOrDefault();
        if (editor is not null)
        {
            _ = editor.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                () =>
                {
                    _ = editor.Focus();
                    editor.SelectAll();
                });
        }

        eventArgs.Handled = true;
    }

    private void NameEditor_OnKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (sender is not TextBox { DataContext: IoPointItem point } editor ||
            DataContext is not IoMonitorViewModel viewModel)
        {
            return;
        }

        if (eventArgs.Key == Key.Enter)
        {
            editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            _ = viewModel.SaveNameCommand.Execute(point);
            eventArgs.Handled = true;
        }
        else if (eventArgs.Key == Key.Escape)
        {
            viewModel.CancelEditNameCommand.Execute(point);
            eventArgs.Handled = true;
        }
    }

    private void NameEditor_OnLostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs eventArgs)
    {
        if (sender is TextBox { DataContext: IoPointItem { IsEditing: true } point } editor &&
            DataContext is IoMonitorViewModel viewModel)
        {
            editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            _ = viewModel.SaveNameCommand.Execute(point);
        }
    }
}
