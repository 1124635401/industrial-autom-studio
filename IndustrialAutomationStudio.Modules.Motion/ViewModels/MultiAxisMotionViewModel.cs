using System.Collections.ObjectModel;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class MultiAxisMotionViewModel : BindableBase, INavigationAware
{
    private readonly IAxisConfigService _axisConfigService;
    private readonly IAxisGroupConfigService _groupConfigService;
    private readonly JogModuleFactory _jogModuleFactory = new();
    private IReadOnlyDictionary<AxisAddress, AxisConfig> _axes =
        new Dictionary<AxisAddress, AxisConfig>();
    private AxisGroupOptionViewModel? _selectedGroup;
    private JogDirectionViewModel? _activeDirection;
    private string _statusMessage = PreviewIdleMessage;
    private bool _isBusy;
    private bool _hasLoadError;
    private readonly HashSet<AxisAddress> _missingAxes = [];

    private const string PreviewIdleMessage = "预览模式：未执行运动指令。";

    public MultiAxisMotionViewModel(
        IAxisConfigService axisConfigService,
        IAxisGroupConfigService groupConfigService)
    {
        _axisConfigService = axisConfigService;
        _groupConfigService = groupConfigService;
        LoadCommand = new AsyncDelegateCommand(LoadAsync);
        BeginPreviewCommand = new DelegateCommand<JogDirectionViewModel>(
            BeginPreview);
        EndPreviewCommand = new DelegateCommand<JogDirectionViewModel>(
            EndPreview);
    }

    public ObservableCollection<AxisGroupOptionViewModel> Groups { get; } = [];
    public ObservableCollection<JogModuleViewModel> Modules { get; } = [];
    public ObservableCollection<JogModuleViewModel> CenterModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> LinearModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> RotaryModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> AuxiliaryModules { get; } = [];
    public AsyncDelegateCommand LoadCommand { get; }
    public DelegateCommand<JogDirectionViewModel> BeginPreviewCommand { get; }
    public DelegateCommand<JogDirectionViewModel> EndPreviewCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;
    public bool HasGroups => Groups.Count > 0;
    public bool HasModules => Modules.Count > 0;
    public bool HasCenterModules => CenterModules.Count > 0;
    public bool HasLinearModules => LinearModules.Count > 0;
    public bool HasRotaryModules => RotaryModules.Count > 0;
    public bool HasAuxiliaryModules => AuxiliaryModules.Count > 0;

    public bool HasLoadError
    {
        get => _hasLoadError;
        private set => SetProperty(ref _hasLoadError, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int SelectedAxisCount => SelectedGroup?.AxisCount ?? 0;

    public AxisGroupOptionViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            ClearPreview(updateStatus: false);
            if (SetProperty(ref _selectedGroup, value))
            {
                RaisePropertyChanged(nameof(SelectedAxisCount));
                RebuildModules();
            }
        }
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        HasLoadError = false;
        StatusMessage = "正在加载分组与轴配置...";
        ClearPreview(updateStatus: false);
        try
        {
            var axisTask = _axisConfigService.LoadAsync();
            var groupTask = _groupConfigService.LoadAsync();
            await Task.WhenAll(axisTask, groupTask);

            _axes = axisTask.Result
                .GroupBy(axis => axis.Address)
                .ToDictionary(group => group.Key, group => group.First());

            Groups.Clear();
            foreach (var group in groupTask.Result)
            {
                Groups.Add(new AxisGroupOptionViewModel(group));
            }

            RaisePropertyChanged(nameof(HasGroups));
            SelectedGroup = Groups.FirstOrDefault();
            if (SelectedGroup is null)
            {
                ClearModules();
                RaiseModuleStateChanged();
                StatusMessage = "没有可用分组，请先在分组管理中创建分组。";
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Groups.Clear();
            ClearModules();
            _selectedGroup = null;
            RaisePropertyChanged(nameof(SelectedGroup));
            RaisePropertyChanged(nameof(SelectedAxisCount));
            RaisePropertyChanged(nameof(HasGroups));
            RaiseModuleStateChanged();
            HasLoadError = true;
            StatusMessage = $"加载多轴运动页面失败：{exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void BeginPreview(JogDirectionViewModel? direction)
    {
        if (direction is null || IsBusy)
        {
            return;
        }

        ClearPreview(updateStatus: false);
        _activeDirection = direction;
        direction.IsActive = true;
        StatusMessage = $"预览：{direction.AxisName} {direction.Label}；未执行运动指令。";
    }

    public void EndPreview(JogDirectionViewModel? direction)
    {
        if (_activeDirection is null
            || (direction is not null && !ReferenceEquals(_activeDirection, direction)))
        {
            return;
        }

        ClearPreview(updateStatus: true);
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) =>
        ClearPreview(updateStatus: true);

    private void RebuildModules()
    {
        ClearModules();
        _missingAxes.Clear();
        if (SelectedGroup is null)
        {
            RaiseModuleStateChanged();
            return;
        }

        var result = _jogModuleFactory.Build(SelectedGroup.Config, _axes);
        foreach (var module in result.Modules)
        {
            AddModule(module);
        }

        foreach (var address in result.MissingAxes)
        {
            _missingAxes.Add(address);
        }

        RaiseModuleStateChanged();
        UpdateIdleStatus();
    }

    private JogModuleViewModel CreateModule(AxisGroupMember member) =>
        member.Role switch
        {
            AxisRole.X => CreateLinearHorizontalModule(member),
            AxisRole.Y => CreateLinearVerticalModule(
                member,
                JogModuleRegion.Center),
            AxisRole.Z or AxisRole.V or AxisRole.W =>
                CreateLinearVerticalModule(member, JogModuleRegion.Linear),
            AxisRole.R or AxisRole.U => CreateRotaryModule(member),
            AxisRole.XY => CreatePlanarModule(member),
            _ => CreateUnassignedModule(member)
        };

    private JogModuleViewModel CreatePlanarModule(
        AxisGroupMember x,
        AxisGroupMember y) => new(
        JogModuleKind.Planar,
        JogModuleRegion.Center,
        "平台运动",
        "X / Y",
        [
            Direction(y, 1, "Y+", "↑"),
            Direction(x, -1, "X−", "←"),
            Direction(x, 1, "X+", "→"),
            Direction(y, -1, "Y−", "↓")
        ]);

    private JogModuleViewModel CreatePlanarModule(AxisGroupMember member) => new(
        JogModuleKind.Planar,
        JogModuleRegion.Center,
        AxisName(member),
        "XY",
        [
            Direction(member, 1, "Y+", "↑"),
            Direction(member, -1, "X−", "←"),
            Direction(member, 1, "X+", "→"),
            Direction(member, -1, "Y−", "↓")
        ]);

    private JogModuleViewModel CreateLinearHorizontalModule(AxisGroupMember member) => new(
        JogModuleKind.LinearHorizontal,
        JogModuleRegion.Center,
        AxisName(member),
        member.Role.ToString(),
        [
            Direction(member, -1, $"{member.Role}−", "←"),
            Direction(member, 1, $"{member.Role}+", "→")
        ]);

    private JogModuleViewModel CreateLinearVerticalModule(
        AxisGroupMember member,
        JogModuleRegion region) => new(
        JogModuleKind.LinearVertical,
        region,
        AxisName(member),
        member.Role.ToString(),
        [
            Direction(member, 1, $"{member.Role}+", "↑"),
            Direction(member, -1, $"{member.Role}−", "↓")
        ]);

    private JogModuleViewModel CreateRotaryModule(AxisGroupMember member) => new(
        JogModuleKind.Rotary,
        JogModuleRegion.Rotary,
        AxisName(member),
        member.Role.ToString(),
        [
            Direction(member, 1, $"{member.Role}+", "↻"),
            Direction(member, -1, $"{member.Role}−", "↺")
        ]);

    private JogModuleViewModel CreateUnassignedModule(AxisGroupMember member) => new(
        JogModuleKind.LinearHorizontal,
        JogModuleRegion.Auxiliary,
        AxisName(member),
        "未分配",
        [
            Direction(member, -1, "−", "←"),
            Direction(member, 1, "+", "→")
        ]);

    private JogDirectionViewModel Direction(
        AxisGroupMember member,
        int direction,
        string label,
        string symbol) => new(
        member.Address,
        member.Role,
        direction,
        AxisName(member),
        label,
        symbol);

    private string AxisName(AxisGroupMember member) =>
        _axes.TryGetValue(member.Address, out var axis)
            ? axis.AxisName
            : MissingAxisName(member);

    private string MissingAxisName(AxisGroupMember member)
    {
        _missingAxes.Add(member.Address);
        return $"轴 {member.Address.CardNo}:{member.Address.AxisNo}";
    }

    private void AddModule(JogModuleViewModel module)
    {
        Modules.Add(module);
        RegionCollection(module.Region).Add(module);
    }

    private ObservableCollection<JogModuleViewModel> RegionCollection(
        JogModuleRegion region) => region switch
        {
            JogModuleRegion.Center => CenterModules,
            JogModuleRegion.Linear => LinearModules,
            JogModuleRegion.Rotary => RotaryModules,
            JogModuleRegion.Auxiliary => AuxiliaryModules,
            _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
        };

    private void ClearModules()
    {
        Modules.Clear();
        CenterModules.Clear();
        LinearModules.Clear();
        RotaryModules.Clear();
        AuxiliaryModules.Clear();
    }

    private void RaiseModuleStateChanged()
    {
        RaisePropertyChanged(nameof(HasModules));
        RaisePropertyChanged(nameof(HasCenterModules));
        RaisePropertyChanged(nameof(HasLinearModules));
        RaisePropertyChanged(nameof(HasRotaryModules));
        RaisePropertyChanged(nameof(HasAuxiliaryModules));
    }

    private void ClearPreview(bool updateStatus)
    {
        if (_activeDirection is not null)
        {
            _activeDirection.IsActive = false;
            _activeDirection = null;
        }

        if (updateStatus)
        {
            UpdateIdleStatus();
        }
    }

    private void UpdateIdleStatus()
    {
        if (SelectedGroup is null)
        {
            StatusMessage = Groups.Count == 0
                ? "没有可用分组，请先在分组管理中创建分组。"
                : PreviewIdleMessage;
            return;
        }

        if (Modules.Count == 0)
        {
            StatusMessage = "当前分组未分配轴，请先在分组管理中添加轴。";
            return;
        }

        StatusMessage = _missingAxes.Count > 0
            ? $"{PreviewIdleMessage} {_missingAxes.Count} 个轴缺少配置，已使用地址显示。"
            : PreviewIdleMessage;
    }

    private static int RoleOrder(AxisRole role) => role switch
    {
        AxisRole.XY => 0,
        AxisRole.X => 1,
        AxisRole.Y => 2,
        AxisRole.Z => 3,
        AxisRole.R => 4,
        AxisRole.U => 5,
        AxisRole.V => 6,
        AxisRole.W => 7,
        _ => 8
    };
}
