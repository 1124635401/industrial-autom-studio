using System.Windows;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.Dialogs;

namespace IndustrialAutomationStudio.Modules.Motion.Views.Dialogs;

public partial class AddAxisDialog : Window
{
    public AddAxisDialog()
        : this(new AddAxisDialogViewModel())
    {
    }

    public AddAxisDialog(AddAxisDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        Closed += (_, _) => viewModel.RequestClose -= OnRequestClose;
    }

    private void OnRequestClose(bool result) => DialogResult = result;
}
