using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Prism.Commands;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Dialogs;

public enum ConfirmDialogIcon
{
    None,
    Info,
    Warning,
    Error,
    Question
}

public enum ConfirmDialogResult
{
    None,
    Ok,
    Yes,
    No,
    Cancel
}

public sealed record ConfirmDialogButton(
    string Text,
    ConfirmDialogResult Result,
    bool IsPrimary = false,
    bool IsDanger = false);

public sealed partial class MotionConfirmDialog : Window
{
    public string DialogTitle { get; set; } = string.Empty;
    public string DialogMessage { get; set; } = string.Empty;
    public ConfirmDialogIcon DialogIcon { get; set; }
    public IReadOnlyList<ConfirmDialogButton> Buttons { get; set; } = [];
    public ConfirmDialogResult Result { get; private set; } = ConfirmDialogResult.None;

    public Visibility IconVisibility { get; private set; } = Visibility.Collapsed;
    public string IconGlyph { get; private set; } = string.Empty;
    public Brush? IconBackBrush { get; private set; }
    public Brush? IconForeBrush { get; private set; }

    public DelegateCommand<ConfirmDialogResult?> ChooseCommand { get; }

    public MotionConfirmDialog()
    {
        InitializeComponent();
        DataContext = this;
        ChooseCommand = new DelegateCommand<ConfirmDialogResult?>(OnChoose);
    }

    public void ApplyIcon()
    {
        switch (DialogIcon)
        {
            case ConfirmDialogIcon.Warning:
                IconVisibility = Visibility.Visible;
                IconGlyph = "!";
                IconBackBrush = (Brush)FindResource("MotionWarningLightBrush");
                IconForeBrush = (Brush)FindResource("MotionDangerBrush");
                break;
            case ConfirmDialogIcon.Error:
                IconVisibility = Visibility.Visible;
                IconGlyph = "×";
                IconBackBrush = (Brush)FindResource("MotionDangerLightBrush");
                IconForeBrush = (Brush)FindResource("MotionDangerBrush");
                break;
            case ConfirmDialogIcon.Question:
            case ConfirmDialogIcon.Info:
                IconVisibility = Visibility.Visible;
                IconGlyph = DialogIcon == ConfirmDialogIcon.Question ? "?" : "i";
                IconBackBrush = (Brush)FindResource("MotionPrimaryLightBrush");
                IconForeBrush = (Brush)FindResource("MotionBrush.Primary");
                break;
            default:
                IconVisibility = Visibility.Collapsed;
                IconGlyph = string.Empty;
                break;
        }
    }

    private void OnChoose(ConfirmDialogResult? result)
    {
        if (result is null)
        {
            return;
        }

        Result = result.Value;
        DialogResult = true;
    }

    public static ConfirmDialogResult Show(
        Window? owner,
        string title,
        string message,
        ConfirmDialogIcon icon,
        IReadOnlyList<ConfirmDialogButton> buttons)
    {
        var dialog = new MotionConfirmDialog
        {
            Owner = owner,
            DialogTitle = title,
            DialogMessage = message,
            DialogIcon = icon,
            Buttons = buttons
        };
        dialog.ApplyIcon();
        dialog.ShowDialog();
        return dialog.Result;
    }
}

public sealed class ConfirmDialogButtonStyleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ConfirmDialogButton button)
        {
            return null;
        }

        var key = button.IsDanger
            ? "MotionDangerOutlineButton"
            : button.IsPrimary
                ? "MotionPrimaryButton"
                : "MotionSecondaryButton";

        return Application.Current?.TryFindResource(key);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
