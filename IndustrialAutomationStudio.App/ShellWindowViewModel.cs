using Prism.Mvvm;

namespace IndustrialAutomationStudio.App;

public sealed class ShellWindowViewModel : BindableBase
{
    public string Title => "Industrial Automation Studio 工业自动化调试平台";
    public string StatusText => "准备就绪";
    public string VersionText => $"v{typeof(ShellWindowViewModel).Assembly.GetName().Version}";
}
