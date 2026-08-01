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
        var result = MotionConfirmDialog.Show(
            Application.Current?.MainWindow,
            "未保存的更改",
            "当前配置尚未保存。是否先保存再继续？",
            ConfirmDialogIcon.Warning,
            new ConfirmDialogButton[]
            {
                new("保存并继续", ConfirmDialogResult.Yes, IsPrimary: true),
                new("不保存", ConfirmDialogResult.No),
                new("取消", ConfirmDialogResult.Cancel)
            });
        return Task.FromResult(result switch
        {
            ConfirmDialogResult.Yes => ConfigurationPromptResult.SaveAndContinue,
            ConfirmDialogResult.No => ConfigurationPromptResult.DiscardAndContinue,
            _ => ConfigurationPromptResult.Cancel
        });
    }

    public Task<bool> ConfirmDeleteAsync(AxisConfig axis)
    {
        var result = MotionConfirmDialog.Show(
            Application.Current?.MainWindow,
            "删除轴配置",
            $"确定从当前配置中删除轴“{axis.AxisName}”（{axis.Address.CardNo}/{axis.Address.AxisNo}）吗？",
            ConfirmDialogIcon.Error,
            new ConfirmDialogButton[]
            {
                new("删除", ConfirmDialogResult.Yes, IsDanger: true),
                new("取消", ConfirmDialogResult.No)
            });
        return Task.FromResult(result == ConfirmDialogResult.Yes);
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
