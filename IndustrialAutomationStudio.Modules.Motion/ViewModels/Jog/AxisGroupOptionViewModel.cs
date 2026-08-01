using IndustrialAutomationStudio.Modules.Motion.Models;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels.Jog;

public sealed class AxisGroupOptionViewModel
{
    public AxisGroupOptionViewModel(AxisGroupConfig config)
    {
        Config = config;
    }

    public AxisGroupConfig Config { get; }
    public string Id => Config.Id;
    public string Name => Config.Name;
    public int AxisCount => Config.Members.Count;
}
