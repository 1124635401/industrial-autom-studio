using System.Windows;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.Dialogs;
using IndustrialAutomationStudio.Modules.Motion.Views.Dialogs;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class WpfConfigurationPromptService : IConfigurationPromptService
{
    public Task<ConfigurationPromptResult> ConfirmUnsavedChangesAsync()
    {
        var result = MessageBox.Show(
            Application.Current?.MainWindow,
            "当前配置尚未保存。是否先保存再继续？",
            "未保存的更改",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return Task.FromResult(result switch
        {
            MessageBoxResult.Yes => ConfigurationPromptResult.SaveAndContinue,
            MessageBoxResult.No => ConfigurationPromptResult.DiscardAndContinue,
            _ => ConfigurationPromptResult.Cancel
        });
    }

    public Task<bool> ConfirmDeleteAsync(AxisConfig axis)
    {
        var result = MessageBox.Show(
            Application.Current?.MainWindow,
            $"确定从当前配置中删除轴“{axis.AxisName}”（{axis.Address.CardNo}/{axis.Address.AxisNo}）吗？",
            "删除轴配置",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<AxisConfig?> ShowAddAxisAsync(AxisAddress suggestedAddress, string suggestedName)
    {
        var viewModel = new AddAxisDialogViewModel(suggestedAddress, suggestedName);
        var dialog = new AddAxisDialog(viewModel)
        {
            Owner = Application.Current?.MainWindow
        };
        return Task.FromResult(dialog.ShowDialog() == true ? viewModel.Result : null);
    }
}
