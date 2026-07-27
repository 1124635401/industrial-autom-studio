using System.Collections.ObjectModel;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.MultiAxis;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.PointDebug;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class PointDebugViewModel : BindableBase, INavigationAware
{
    private readonly IAxisConfigService _axisConfigService;
    private readonly IAxisGroupConfigService _groupConfigService;
    private readonly IPointPositionRepository _pointRepository;
    private readonly IMotionExecutionService _motionExecution;
    private readonly JogModuleFactory _jogModuleFactory;
    private readonly SemaphoreSlim _pointPersistenceGate = new(1, 1);
    private IReadOnlyDictionary<AxisAddress, AxisConfig> _axes =
        new Dictionary<AxisAddress, AxisConfig>();
    private List<PositionPoint> _allPoints = [];
    private AxisGroupOptionViewModel? _selectedGroup;
    private JogDirectionViewModel? _activeDirection;
    private double _jogSpeed = 10;
    private string _statusMessage = "准备就绪";
    private bool _isBusy;
    private bool _isMotionActive;
    private bool _pointMotionPending;
    private DateTimeOffset _pointMoveEarliestCompletion;
    private DateTimeOffset _pointMoveStarted;
    private PointRowViewModel? _activePoint;
    private IReadOnlyDictionary<AxisAddress, double> _activeTargets =
        new Dictionary<AxisAddress, double>();

    public PointDebugViewModel(
        IAxisConfigService axisConfigService,
        IAxisGroupConfigService groupConfigService,
        IPointPositionRepository pointRepository,
        IMotionExecutionService motionExecution,
        JogModuleFactory jogModuleFactory)
    {
        _axisConfigService = axisConfigService;
        _groupConfigService = groupConfigService;
        _pointRepository = pointRepository;
        _motionExecution = motionExecution;
        _jogModuleFactory = jogModuleFactory;

        LoadCommand = new AsyncDelegateCommand(LoadAsync);
        RefreshPositionsCommand = new AsyncDelegateCommand(RefreshPositionsAsync);
        RecordCurrentPositionCommand = new AsyncDelegateCommand(
            RecordCurrentPositionAsync,
            () => IsInteractionEnabled && SelectedGroup is not null);
        BeginEditPointCommand = new DelegateCommand<PointRowViewModel>(
            BeginEditPoint,
            row => row is not null && IsInteractionEnabled);
        SavePointCommand = new AsyncDelegateCommand<PointRowViewModel>(
            SavePointAsync,
            row => row is not null && IsInteractionEnabled);
        CancelEditPointCommand = new DelegateCommand<PointRowViewModel>(CancelEditPoint);
        DeletePointCommand = new AsyncDelegateCommand<PointRowViewModel>(
            DeletePointAsync,
            row => row is not null && IsInteractionEnabled);
        LocatePointCommand = new AsyncDelegateCommand<PointRowViewModel>(
            LocatePointAsync,
            row => row is { IsCompatible: true, IsEditing: false } && IsInteractionEnabled);
        StartJogCommand = new AsyncDelegateCommand<JogDirectionViewModel>(
            StartJogAsync,
            direction => direction is not null && IsInteractionEnabled);
        StopJogCommand = new AsyncDelegateCommand<JogDirectionViewModel>(StopJogAsync);
        StopGroupCommand = new AsyncDelegateCommand(StopGroupAsync);
    }

    public ObservableCollection<AxisGroupOptionViewModel> Groups { get; } = [];
    public ObservableCollection<AxisPositionReadoutViewModel> PositionReadouts { get; } = [];
    public ObservableCollection<PointRowViewModel> Points { get; } = [];
    public ObservableCollection<JogModuleViewModel> Modules { get; } = [];
    public ObservableCollection<JogModuleViewModel> CenterModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> LinearModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> RotaryModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> AuxiliaryModules { get; } = [];

    public AsyncDelegateCommand LoadCommand { get; }
    public AsyncDelegateCommand RefreshPositionsCommand { get; }
    public AsyncDelegateCommand RecordCurrentPositionCommand { get; }
    public DelegateCommand<PointRowViewModel> BeginEditPointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> SavePointCommand { get; }
    public DelegateCommand<PointRowViewModel> CancelEditPointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> DeletePointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> LocatePointCommand { get; }
    public AsyncDelegateCommand<JogDirectionViewModel> StartJogCommand { get; }
    public AsyncDelegateCommand<JogDirectionViewModel> StopJogCommand { get; }
    public AsyncDelegateCommand StopGroupCommand { get; }

    public AxisGroupOptionViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (IsMotionActive || !SetProperty(ref _selectedGroup, value))
            {
                return;
            }

            RebuildSelectedGroup();
            RaisePropertyChanged(nameof(SelectedAxisCount));
            RaiseCommandStates();
        }
    }

    public int SelectedAxisCount => SelectedGroup?.AxisCount ?? 0;
    public bool HasGroups => Groups.Count > 0;
    public bool HasPoints => Points.Count > 0;
    public bool HasCenterModules => CenterModules.Count > 0;
    public bool HasLinearModules => LinearModules.Count > 0;
    public bool HasRotaryModules => RotaryModules.Count > 0;
    public bool HasAuxiliaryModules => AuxiliaryModules.Count > 0;

    public double JogSpeed
    {
        get => _jogSpeed;
        set => SetProperty(ref _jogSpeed, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(IsInteractionEnabled));
                RaiseCommandStates();
            }
        }
    }

    public bool IsMotionActive
    {
        get => _isMotionActive;
        private set
        {
            if (SetProperty(ref _isMotionActive, value))
            {
                RaisePropertyChanged(nameof(IsInteractionEnabled));
                RaisePropertyChanged(nameof(MotionStateText));
                RaiseCommandStates();
            }
        }
    }

    public bool IsInteractionEnabled => !IsBusy && !IsMotionActive;
    public string MotionStateText => IsMotionActive ? "运动中" : "就绪";
    public string ConnectionStateText =>
        _motionExecution.IsMotionAvailable ? "已连接" : "未连接";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "正在加载点位与轴分组...";
        try
        {
            var axesTask = _axisConfigService.LoadAsync(cancellationToken);
            var groupsTask = _groupConfigService.LoadAsync(cancellationToken);
            var pointsTask = _pointRepository.LoadAsync(cancellationToken);
            await Task.WhenAll(axesTask, groupsTask, pointsTask);
            RaisePropertyChanged(nameof(ConnectionStateText));

            _axes = axesTask.Result
                .GroupBy(axis => axis.Address)
                .ToDictionary(values => values.Key, values => values.First());
            _allPoints = pointsTask.Result.Select(ClonePoint).ToList();

            Groups.Clear();
            foreach (var group in groupsTask.Result)
            {
                Groups.Add(new AxisGroupOptionViewModel(group));
            }

            RaisePropertyChanged(nameof(HasGroups));
            SelectedGroup = Groups.FirstOrDefault();
            if (SelectedGroup is null)
            {
                ClearSelectedGroup();
                StatusMessage = "没有可用分组，请先创建轴分组。";
            }
            else
            {
                StatusMessage = $"已加载 {Points.Count} 个点位。";
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ClearSelectedGroup();
            StatusMessage = $"加载点位调试页面失败：{exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshPositionsAsync(CancellationToken cancellationToken = default)
    {
        var configs = SelectedAxisConfigs();
        if (configs.Count == 0)
        {
            return;
        }

        try
        {
            RaisePropertyChanged(nameof(ConnectionStateText));
            var states = await _motionExecution.ReadAxisStatesAsync(
                configs,
                cancellationToken);
            var byAddress = states.ToDictionary(state => state.Address);
            foreach (var readout in PositionReadouts)
            {
                if (byAddress.TryGetValue(readout.Address, out var state))
                {
                    readout.Position = state.ActualPosition;
                }
            }

            var unsafeState = states.FirstOrDefault(IsUnsafeState);
            if (IsMotionActive && unsafeState is not null)
            {
                await EmergencyStopForFaultAsync(
                    $"轴 {unsafeState.Address.CardNo}:{unsafeState.Address.AxisNo} 状态异常",
                    cancellationToken);
                return;
            }

            if (_pointMotionPending &&
                DateTimeOffset.UtcNow >= _pointMoveEarliestCompletion &&
                states.Count == configs.Count)
            {
                var configByAddress = configs.ToDictionary(axis => axis.Address);
                var reached = states.All(state =>
                    _activeTargets.TryGetValue(state.Address, out var target) &&
                    !state.IsMoving &&
                    Math.Abs(state.ActualPosition - target) <=
                    Math.Max(0, configByAddress[state.Address].InPositionError));
                if (reached)
                {
                    _pointMotionPending = false;
                    _activePoint?.MarkCompleted();
                    _activePoint = null;
                    _activeTargets = new Dictionary<AxisAddress, double>();
                    IsMotionActive = false;
                    StatusMessage = "点位定位完成。";
                    return;
                }

                var timedOut = states.Any(state =>
                    _activeTargets.TryGetValue(state.Address, out var target) &&
                    Math.Abs(state.ActualPosition - target) >
                    Math.Max(0, configByAddress[state.Address].InPositionError) &&
                    DateTimeOffset.UtcNow - _pointMoveStarted >=
                    TimeSpan.FromSeconds(Math.Max(
                        0.1,
                        configByAddress[state.Address].InPositionTimeout)));
                if (timedOut)
                {
                    await EmergencyStopForFaultAsync("点位定位超时", cancellationToken);
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (IsMotionActive)
            {
                await EmergencyStopForFaultAsync(
                    $"读取实时位置失败：{exception.Message}",
                    CancellationToken.None);
            }
            else
            {
                StatusMessage = $"读取实时位置失败：{exception.Message}";
            }
        }
    }

    public async Task StartJogAsync(
        JogDirectionViewModel direction,
        CancellationToken cancellationToken = default)
    {
        if (!IsInteractionEnabled ||
            !_axes.TryGetValue(direction.Address, out var axis))
        {
            return;
        }

        _activeDirection = direction;
        direction.IsActive = true;
        IsMotionActive = true;
        StatusMessage = $"Jog：{direction.AxisName} {direction.Label}";
        try
        {
            await _motionExecution.StartJogAsync(
                axis,
                direction.Direction,
                JogSpeed,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (ReferenceEquals(_activeDirection, direction))
            {
                direction.IsActive = false;
                _activeDirection = null;
                IsMotionActive = false;
            }

            StatusMessage = $"Jog 启动失败：{exception.Message}";
        }
    }

    public async Task StopJogAsync(
        JogDirectionViewModel direction,
        CancellationToken cancellationToken = default)
    {
        if (_activeDirection is null ||
            !ReferenceEquals(_activeDirection, direction))
        {
            return;
        }

        var stopped = false;
        try
        {
            await _motionExecution.StopAsync(
                [direction.Address],
                MotionStopMode.Smooth,
                cancellationToken);
            stopped = true;
            StatusMessage = $"{direction.AxisName} 已停止。";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"停止失败：{exception.Message}";
        }
        if (stopped)
        {
            direction.IsActive = false;
            _activeDirection = null;
            IsMotionActive = false;
        }
    }

    public async Task RecordCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        var configs = SelectedAxisConfigs();
        if (configs.Count == 0)
        {
            StatusMessage = "当前分组没有可读取的轴。";
            return;
        }

        try
        {
            var states = await _motionExecution.ReadAxisStatesAsync(
                configs,
                cancellationToken);
            var byAddress = states.ToDictionary(state => state.Address);
            if (!configs.Select(axis => axis.Address).ToHashSet().SetEquals(byAddress.Keys))
            {
                throw new InvalidOperationException("未能读取当前分组的完整实时位置。");
            }

            foreach (var readout in PositionReadouts)
            {
                readout.Position = byAddress[readout.Address].ActualPosition;
            }

            _ = RecordCurrentPosition();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"记录当前位置失败：{exception.Message}";
        }
    }

    public PointRowViewModel RecordCurrentPosition()
    {
        var group = SelectedGroup?.Config
            ?? throw new InvalidOperationException("请先选择分组。");
        var name = NextPointName(group.Id);
        var point = new PositionPoint
        {
            Id = Guid.NewGuid().ToString("N"),
            GroupId = group.Id,
            Name = name,
            Speed = 50,
            AxisPositions = PositionReadouts.Select(readout => new PointAxisPosition
            {
                Address = readout.Address,
                Position = readout.Position
            }).ToList()
        };
        var row = CreatePointRow(point, group, isNew: true);
        Points.Add(row);
        RaisePropertyChanged(nameof(HasPoints));
        StatusMessage = "已记录当前位置，请修改名称和速度后保存。";
        return row;
    }

    public void BeginEditPoint(PointRowViewModel row) => row.BeginEdit();

    public async Task SavePointAsync(
        PointRowViewModel row,
        CancellationToken cancellationToken = default)
    {
        var error = ValidatePoint(row);
        if (error is not null)
        {
            StatusMessage = error;
            return;
        }

        await _pointPersistenceGate.WaitAsync(cancellationToken);
        try
        {
            var currentError = ValidatePoint(row);
            if (currentError is not null)
            {
                StatusMessage = currentError;
                return;
            }

            var model = row.ToModel();
            var updated = _allPoints.Select(ClonePoint).ToList();
            var index = updated.FindIndex(point =>
                string.Equals(point.Id, model.Id, StringComparison.Ordinal));
            if (index >= 0)
            {
                updated[index] = model;
            }
            else
            {
                updated.Add(model);
            }

            await _pointRepository.SaveAsync(updated, cancellationToken);
            _allPoints = updated;
            row.CommitEdit();
            StatusMessage = $"点位“{row.Name}”已保存。";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"保存点位失败：{exception.Message}";
        }
        finally
        {
            _pointPersistenceGate.Release();
        }
    }

    public void CancelEditPoint(PointRowViewModel row)
    {
        if (row.IsNew)
        {
            Points.Remove(row);
            RaisePropertyChanged(nameof(HasPoints));
        }
        else
        {
            row.CancelEdit();
        }
    }

    public async Task DeletePointAsync(
        PointRowViewModel row,
        CancellationToken cancellationToken = default)
    {
        await _pointPersistenceGate.WaitAsync(cancellationToken);
        try
        {
            var updated = _allPoints
                .Where(point => !string.Equals(point.Id, row.Id, StringComparison.Ordinal))
                .Select(ClonePoint)
                .ToList();
            var removed = updated.Count != _allPoints.Count;
            if (removed)
            {
                await _pointRepository.SaveAsync(updated, cancellationToken);
            }

            _allPoints = updated;
            Points.Remove(row);
            RaisePropertyChanged(nameof(HasPoints));
            StatusMessage = $"点位“{row.Name}”已删除。";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"删除点位失败：{exception.Message}";
        }
        finally
        {
            _pointPersistenceGate.Release();
        }
    }

    public async Task LocatePointAsync(
        PointRowViewModel row,
        CancellationToken cancellationToken = default)
    {
        var group = SelectedGroup?.Config;
        if (group is null)
        {
            return;
        }

        row.UpdateCompatibility(group);
        if (!row.IsCompatible)
        {
            StatusMessage = $"点位“{row.Name}”分组已变更，无法定位。";
            return;
        }

        IsMotionActive = true;
        _pointMotionPending = true;
        _pointMoveEarliestCompletion = DateTimeOffset.UtcNow.AddMilliseconds(400);
        _pointMoveStarted = DateTimeOffset.UtcNow;
        var model = row.ToModel();
        _activePoint = row;
        _activeTargets = model.AxisPositions.ToDictionary(
            position => position.Address,
            position => position.Position);
        row.MarkRunning();
        try
        {
            await _motionExecution.MoveToPointAsync(
                group,
                SelectedAxisConfigs().ToDictionary(axis => axis.Address),
                model,
                cancellationToken);
            StatusMessage = $"已下发点位“{row.Name}”定位指令。";
        }
        catch (MotionFailStopException exception)
        {
            _pointMotionPending = false;
            row.MarkFailed(exception.Message);
            StatusMessage = exception.Message;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _pointMotionPending = false;
            IsMotionActive = false;
            row.MarkFailed(exception.Message);
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            StatusMessage = $"定位失败：{exception.Message}";
        }
    }

    public async Task StopGroupAsync(CancellationToken cancellationToken = default)
    {
        var addresses = SelectedGroup?.Config.Members
            .Select(member => member.Address)
            .ToArray() ?? [];
        if (addresses.Length == 0)
        {
            return;
        }

        var stopped = false;
        try
        {
            await _motionExecution.StopAsync(
                addresses,
                MotionStopMode.Smooth,
                cancellationToken);
            stopped = true;
            StatusMessage = "当前分组已停止。";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"停止失败：{exception.Message}";
        }
        if (stopped)
        {
            _pointMotionPending = false;
            _activePoint?.MarkStopped();
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            if (_activeDirection is not null)
            {
                _activeDirection.IsActive = false;
                _activeDirection = null;
            }

            IsMotionActive = false;
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => _ = LoadAsync();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) => _ = StopGroupAsync();

    private void RebuildSelectedGroup()
    {
        ClearSelectedGroup();
        var group = SelectedGroup?.Config;
        if (group is null)
        {
            return;
        }

        var orderedMembers = group.Members
            .OrderBy(member => RoleOrder(member.Role))
            .ThenBy(member => member.Address.CardNo)
            .ThenBy(member => member.Address.AxisNo)
            .ToArray();
        foreach (var member in orderedMembers)
        {
            if (!_axes.TryGetValue(member.Address, out var axis))
            {
                continue;
            }

            PositionReadouts.Add(new AxisPositionReadoutViewModel(
                member.Address,
                member.Role,
                RoleLabel(member, axis),
                DisplayUnit(axis.Unit)));
        }

        var build = _jogModuleFactory.Build(group, _axes);
        foreach (var module in build.Modules)
        {
            Modules.Add(module);
            RegionCollection(module.Region).Add(module);
        }

        foreach (var point in _allPoints.Where(point =>
                     string.Equals(point.GroupId, group.Id, StringComparison.Ordinal)))
        {
            Points.Add(CreatePointRow(point, group));
        }

        RaiseCollectionStates();
    }

    private PointRowViewModel CreatePointRow(
        PositionPoint point,
        AxisGroupConfig group,
        bool isNew = false)
    {
        var positions = point.AxisPositions.ToDictionary(value => value.Address);
        var members = group.Members.ToDictionary(member => member.Address);
        var addresses = isNew
            ? group.Members
                .OrderBy(member => RoleOrder(member.Role))
                .Select(member => member.Address)
            : point.AxisPositions
                .OrderBy(value => members.TryGetValue(value.Address, out var member)
                    ? RoleOrder(member.Role)
                    : int.MaxValue)
                .ThenBy(value => value.Address.CardNo)
                .ThenBy(value => value.Address.AxisNo)
                .Select(value => value.Address);
        var cells = addresses.Select(address =>
        {
            members.TryGetValue(address, out var member);
            _axes.TryGetValue(address, out var axis);
            var role = member?.Role ?? AxisRole.None;
            var label = axis is null
                ? role == AxisRole.None
                    ? $"{address.CardNo}:{address.AxisNo}"
                    : role.ToString()
                : role == AxisRole.None
                    ? axis.AxisName
                    : role.ToString();
            return new PointAxisCellViewModel(
                address,
                role,
                label,
                axis is null ? string.Empty : DisplayUnit(axis.Unit),
                positions.GetValueOrDefault(address)?.Position ?? double.NaN);
        });
        var row = new PointRowViewModel(point, cells, isNew);
        row.UpdateCompatibility(group);
        if (isNew)
        {
            row.BeginEdit();
        }

        return row;
    }

    private string? ValidatePoint(PointRowViewModel row)
    {
        if (row.HasNumericErrors)
        {
            return "点位中存在无法识别的速度或位置数值。";
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return "点位名称不能为空。";
        }

        if (!double.IsFinite(row.Speed) || row.Speed <= 0)
        {
            return "点位速度必须是有限正数。";
        }

        if (row.AxisCells.Any(cell => !double.IsFinite(cell.Position)))
        {
            return "点位位置必须是有限数值。";
        }

        foreach (var cell in row.AxisCells)
        {
            if (!_axes.TryGetValue(cell.Address, out var axis))
            {
                return $"轴 {cell.Address.CardNo}:{cell.Address.AxisNo} 缺少配置。";
            }

            if (axis.NegativeSoftLimit is { } negative &&
                cell.Position < negative)
            {
                return $"轴 {axis.AxisName} 的目标位置超出负软限位。";
            }

            if (axis.PositiveSoftLimit is { } positive &&
                cell.Position > positive)
            {
                return $"轴 {axis.AxisName} 的目标位置超出正软限位。";
            }
        }

        var group = SelectedGroup?.Config;
        if (group is null)
        {
            return "请先选择分组。";
        }

        row.UpdateCompatibility(group);
        if (!row.IsCompatible)
        {
            return "点位分组已变更，无法保存。";
        }

        if (_allPoints.Any(point =>
                !string.Equals(point.Id, row.Id, StringComparison.Ordinal) &&
                string.Equals(point.GroupId, row.GroupId, StringComparison.Ordinal) &&
                string.Equals(
                    point.Name.Trim(),
                    row.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
        {
            return "同一分组内的点位名称不能重复。";
        }

        return null;
    }

    private IReadOnlyList<AxisConfig> SelectedAxisConfigs() =>
        SelectedGroup?.Config.Members
            .Where(member => _axes.ContainsKey(member.Address))
            .Select(member => _axes[member.Address])
            .ToArray() ?? [];

    private void ClearSelectedGroup()
    {
        PositionReadouts.Clear();
        Points.Clear();
        Modules.Clear();
        CenterModules.Clear();
        LinearModules.Clear();
        RotaryModules.Clear();
        AuxiliaryModules.Clear();
        RaiseCollectionStates();
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

    private void RaiseCollectionStates()
    {
        RaisePropertyChanged(nameof(HasPoints));
        RaisePropertyChanged(nameof(HasCenterModules));
        RaisePropertyChanged(nameof(HasLinearModules));
        RaisePropertyChanged(nameof(HasRotaryModules));
        RaisePropertyChanged(nameof(HasAuxiliaryModules));
    }

    private void RaiseCommandStates()
    {
        RecordCurrentPositionCommand.RaiseCanExecuteChanged();
        BeginEditPointCommand.RaiseCanExecuteChanged();
        SavePointCommand.RaiseCanExecuteChanged();
        DeletePointCommand.RaiseCanExecuteChanged();
        LocatePointCommand.RaiseCanExecuteChanged();
        StartJogCommand.RaiseCanExecuteChanged();
    }

    private async Task EmergencyStopForFaultAsync(
        string reason,
        CancellationToken cancellationToken)
    {
        var addresses = SelectedGroup?.Config.Members
            .Select(member => member.Address)
            .ToArray() ?? [];
        if (addresses.Length == 0)
        {
            StatusMessage = reason;
            return;
        }

        try
        {
            await _motionExecution.StopAsync(
                addresses,
                MotionStopMode.Emergency,
                cancellationToken);
            _pointMotionPending = false;
            _activePoint?.MarkFailed(reason);
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            if (_activeDirection is not null)
            {
                _activeDirection.IsActive = false;
                _activeDirection = null;
            }

            IsMotionActive = false;
            StatusMessage = $"{reason}，已紧急停止当前分组。";
        }
        catch (Exception stopException) when (stopException is not OperationCanceledException)
        {
            StatusMessage = $"{reason}，且停止失败：{stopException.Message}";
        }
    }

    private bool IsUnsafeState(AxisState state)
    {
        if (state.Alarm ||
            state.PositiveLimit ||
            state.NegativeLimit ||
            state.EmergencyStop ||
            !state.ServoOn ||
            !double.IsFinite(state.ActualPosition))
        {
            return true;
        }

        if (!_axes.TryGetValue(state.Address, out var axis))
        {
            return true;
        }

        if (axis.NegativeSoftLimit is { } negative &&
            state.ActualPosition < negative)
        {
            return true;
        }

        if (axis.PositiveSoftLimit is { } positive &&
            state.ActualPosition > positive)
        {
            return true;
        }

        if (_activeDirection?.Address != state.Address ||
            !double.IsFinite(state.CurrentVelocity) ||
            axis.Deceleration <= 0)
        {
            return false;
        }

        var stoppingDistance =
            state.CurrentVelocity * state.CurrentVelocity / (2 * axis.Deceleration) +
            Math.Max(0, axis.InPositionError);
        return
            (state.CurrentVelocity > 0 &&
             axis.PositiveSoftLimit is { } jogPositive &&
             state.ActualPosition >= jogPositive - stoppingDistance) ||
            (state.CurrentVelocity < 0 &&
             axis.NegativeSoftLimit is { } jogNegative &&
             state.ActualPosition <= jogNegative + stoppingDistance);
    }

    private string NextPointName(string groupId)
    {
        const string baseName = "点位";
        var names = _allPoints
            .Where(point => string.Equals(point.GroupId, groupId, StringComparison.Ordinal))
            .Select(point => point.Name)
            .Concat(Points.Select(point => point.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var index = 1;
        while (names.Contains($"{baseName} {index}"))
        {
            index++;
        }

        return $"{baseName} {index}";
    }

    private static PositionPoint ClonePoint(PositionPoint point) => point with
    {
        AxisPositions = point.AxisPositions.Select(value => value with { }).ToList()
    };

    private static string RoleLabel(AxisGroupMember member, AxisConfig axis) =>
        member.Role == AxisRole.None ? axis.AxisName : member.Role.ToString();

    private static string DisplayUnit(string unit) =>
        string.Equals(unit, "degree", StringComparison.OrdinalIgnoreCase) ? "°" : unit;

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
