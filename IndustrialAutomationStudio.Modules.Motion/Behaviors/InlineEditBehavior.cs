using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace IndustrialAutomationStudio.Modules.Motion.Behaviors;

public static class InlineEditBehavior
{
    public static readonly DependencyProperty BeginEditCommandProperty =
        DependencyProperty.RegisterAttached(
            "BeginEditCommand",
            typeof(ICommand),
            typeof(InlineEditBehavior),
            new PropertyMetadata(null, OnActivationPropertyChanged));

    public static readonly DependencyProperty EditorProperty =
        DependencyProperty.RegisterAttached(
            "Editor",
            typeof(TextBox),
            typeof(InlineEditBehavior),
            new PropertyMetadata(null, OnActivationPropertyChanged));

    public static readonly DependencyProperty SaveCommandProperty =
        DependencyProperty.RegisterAttached(
            "SaveCommand",
            typeof(ICommand),
            typeof(InlineEditBehavior),
            new PropertyMetadata(null, OnEditorCommandChanged));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.RegisterAttached(
            "CancelCommand",
            typeof(ICommand),
            typeof(InlineEditBehavior),
            new PropertyMetadata(null, OnEditorCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(InlineEditBehavior));

    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.RegisterAttached(
            "IsEditing",
            typeof(bool),
            typeof(InlineEditBehavior));

    private static readonly DependencyProperty IsActivationHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsActivationHooked",
            typeof(bool),
            typeof(InlineEditBehavior));

    private static readonly DependencyProperty IsEditorHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsEditorHooked",
            typeof(bool),
            typeof(InlineEditBehavior));

    public static ICommand? GetBeginEditCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(BeginEditCommandProperty);

    public static void SetBeginEditCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(BeginEditCommandProperty, value);

    public static TextBox? GetEditor(DependencyObject element) =>
        (TextBox?)element.GetValue(EditorProperty);

    public static void SetEditor(DependencyObject element, TextBox? value) =>
        element.SetValue(EditorProperty, value);

    public static ICommand? GetSaveCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(SaveCommandProperty);

    public static void SetSaveCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(SaveCommandProperty, value);

    public static ICommand? GetCancelCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(CancelCommandProperty);

    public static void SetCancelCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(CancelCommandProperty, value);

    public static object? GetCommandParameter(DependencyObject element) =>
        element.GetValue(CommandParameterProperty);

    public static void SetCommandParameter(DependencyObject element, object? value) =>
        element.SetValue(CommandParameterProperty, value);

    public static bool GetIsEditing(DependencyObject element) =>
        (bool)element.GetValue(IsEditingProperty);

    public static void SetIsEditing(DependencyObject element, bool value) =>
        element.SetValue(IsEditingProperty, value);

    private static void OnActivationPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not TextBlock textBlock)
        {
            return;
        }

        var shouldHook = GetBeginEditCommand(textBlock) is not null &&
                         GetEditor(textBlock) is not null;
        var isHooked = (bool)textBlock.GetValue(IsActivationHookedProperty);
        if (shouldHook == isHooked)
        {
            return;
        }

        if (shouldHook)
        {
            textBlock.MouseLeftButtonDown += OnActivationMouseLeftButtonDown;
        }
        else
        {
            textBlock.MouseLeftButtonDown -= OnActivationMouseLeftButtonDown;
        }

        textBlock.SetValue(IsActivationHookedProperty, shouldHook);
    }

    private static void OnEditorCommandChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not TextBox editor)
        {
            return;
        }

        var shouldHook = GetSaveCommand(editor) is not null ||
                         GetCancelCommand(editor) is not null;
        var isHooked = (bool)editor.GetValue(IsEditorHookedProperty);
        if (shouldHook == isHooked)
        {
            return;
        }

        if (shouldHook)
        {
            editor.KeyDown += OnEditorKeyDown;
            editor.LostKeyboardFocus += OnEditorLostKeyboardFocus;
        }
        else
        {
            editor.KeyDown -= OnEditorKeyDown;
            editor.LostKeyboardFocus -= OnEditorLostKeyboardFocus;
        }

        editor.SetValue(IsEditorHookedProperty, shouldHook);
    }

    private static void OnActivationMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ClickCount != 2 || sender is not TextBlock textBlock)
        {
            return;
        }

        var command = GetBeginEditCommand(textBlock);
        var parameter = GetCommandParameter(textBlock);
        var editor = GetEditor(textBlock);
        if (command?.CanExecute(parameter) != true || editor is null)
        {
            return;
        }

        command.Execute(parameter);
        _ = editor.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (!editor.IsVisible)
                {
                    return;
                }

                _ = editor.Focus();
                editor.SelectAll();
            }));
        eventArgs.Handled = true;
    }

    private static void OnEditorKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (sender is not TextBox editor)
        {
            return;
        }

        if (eventArgs.Key == Key.Enter)
        {
            UpdateSourceAndExecute(editor, GetSaveCommand(editor));
            eventArgs.Handled = true;
        }
        else if (eventArgs.Key == Key.Escape)
        {
            Execute(editor, GetCancelCommand(editor));
            eventArgs.Handled = true;
        }
    }

    private static void OnEditorLostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs eventArgs)
    {
        if (sender is TextBox editor && GetIsEditing(editor))
        {
            UpdateSourceAndExecute(editor, GetSaveCommand(editor));
        }
    }

    private static void UpdateSourceAndExecute(TextBox editor, ICommand? command)
    {
        editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Execute(editor, command);
    }

    private static void Execute(DependencyObject element, ICommand? command)
    {
        var parameter = GetCommandParameter(element);
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }
}
