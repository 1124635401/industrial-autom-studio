using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.ViewModels;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Controls;

public partial class AxisConfigurationTablePanel : UserControl
{
    private AxisConfigViewModel? _viewModel;

    public AxisConfigurationTablePanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        AttachViewModel(DataContext as AxisConfigViewModel);

    private void OnUnloaded(object sender, RoutedEventArgs e) => AttachViewModel(null);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as AxisConfigViewModel);
        }
    }

    private void AttachViewModel(AxisConfigViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.AxisAdded -= OnAxisAdded;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.AxisAdded += OnAxisAdded;
        }
    }

    private void OnAxisAdded(AxisConfigEditorViewModel axis) =>
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => FocusAxisName(axis)));

    private void FocusAxisName(AxisConfigEditorViewModel axis)
    {
        AxisGrid.ScrollIntoView(axis, AxisNameColumn);
        AxisGrid.UpdateLayout();
        if (AxisGrid.ItemContainerGenerator.ContainerFromItem(axis) is not DataGridRow row)
        {
            return;
        }

        var editor = FindDescendant<TextBox>(row, textBox => Equals(textBox.Tag, "AxisNameEditor"));
        if (editor is null)
        {
            return;
        }

        editor.Focus();
        editor.SelectAll();
    }

    private static T? FindDescendant<T>(DependencyObject parent, Predicate<T> predicate)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match && predicate(match))
            {
                return match;
            }

            var descendant = FindDescendant(child, predicate);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
