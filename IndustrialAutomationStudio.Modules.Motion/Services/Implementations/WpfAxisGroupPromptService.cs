using System.Windows;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class WpfAxisGroupPromptService : IAxisGroupPromptService
{
    public Task<ConfigurationPromptResult> ConfirmUnsavedChangesAsync()
    {
        var result = MessageBox.Show(
            Application.Current?.MainWindow,
            "当前分组存在未保存的修改。\n是：保存并继续　否：放弃修改　取消：留在当前页面",
            "未保存的修改",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return Task.FromResult(result switch
        {
            MessageBoxResult.Yes => ConfigurationPromptResult.SaveAndContinue,
            MessageBoxResult.No => ConfigurationPromptResult.DiscardAndContinue,
            _ => ConfigurationPromptResult.Cancel
        });
    }

    public Task<bool> ConfirmDeleteAsync(string groupName)
    {
        var result = MessageBox.Show(
            Application.Current?.MainWindow,
            $"确定删除分组“{groupName}”吗？\n该操作仅删除分组及其轴关联，不删除轴配置。",
            "删除分组",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
