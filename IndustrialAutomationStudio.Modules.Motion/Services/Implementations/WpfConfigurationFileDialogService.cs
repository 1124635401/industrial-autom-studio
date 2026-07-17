using Microsoft.Win32;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;

namespace IndustrialAutomationStudio.Modules.Motion.Services.Implementations;

public sealed class WpfConfigurationFileDialogService : IConfigurationFileDialogService
{
    private const string JsonFilter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";

    public Task<string?> SelectImportFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入轴配置",
            Filter = JsonFilter,
            CheckFileExists = true,
            Multiselect = false
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }

    public Task<string?> SelectExportFileAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "导出轴配置",
            Filter = JsonFilter,
            DefaultExt = ".json",
            AddExtension = true,
            FileName = $"AxisConfig_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }
}
