using System.Windows;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Views.Dialogs;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class WpfAxisGroupPromptService : IAxisGroupPromptService
{
    public Task<ConfigurationPromptResult> ConfirmUnsavedChangesAsync()
    {
        var result = MotionConfirmDialog.Show(
            Application.Current?.MainWindow,
            "未保存的修改",
            "当前分组存在未保存的修改。",
            ConfirmDialogIcon.Warning,
            new ConfirmDialogButton[]
            {
                new("保存并继续", ConfirmDialogResult.Yes, IsPrimary: true),
                new("放弃修改", ConfirmDialogResult.No),
                new("取消", ConfirmDialogResult.Cancel)
            });
        return Task.FromResult(result switch
        {
            ConfirmDialogResult.Yes => ConfigurationPromptResult.SaveAndContinue,
            ConfirmDialogResult.No => ConfigurationPromptResult.DiscardAndContinue,
            _ => ConfigurationPromptResult.Cancel
        });
    }

    public Task<bool> ConfirmDeleteAsync(string groupName)
    {
        var result = MotionConfirmDialog.Show(
            Application.Current?.MainWindow,
            "删除分组",
            $"确定删除分组“{groupName}”吗？\n该操作仅删除分组及其轴关联，不删除轴配置。",
            ConfirmDialogIcon.Error,
            new ConfirmDialogButton[]
            {
                new("删除", ConfirmDialogResult.Yes, IsDanger: true),
                new("取消", ConfirmDialogResult.No)
            });
        return Task.FromResult(result == ConfirmDialogResult.Yes);
    }
}
