using System.Windows.Controls;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.Views;

public partial class MotionWorkspaceView : UserControl
{
    public MotionWorkspaceView(MotionModuleOptions options)
    {
        InitializeComponent();
        RegionManager.SetRegionName(WorkspaceContent, options.WorkspaceRegionName);
    }
}
