using System.IO;
using IndustrialAutomationStudio.Modules.Motion.Navigation;

namespace IndustrialAutomationStudio.Modules.Motion;

public sealed class MotionModuleOptions
{
    public string ConfigDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IndustrialAutomationStudio",
        "Motion",
        "Configs");

    public string HostRegionName { get; init; } = MotionRegionNames.HostContent;

    public string WorkspaceRegionName { get; init; } = MotionRegionNames.WorkspaceContent;

    public string DefaultDriverKey { get; init; } = "Mock";

    public bool AutoConnectOnStartup { get; init; }
}
