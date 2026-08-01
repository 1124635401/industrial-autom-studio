using System.Collections.ObjectModel;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.ViewModels.Jog;
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
    private static readonly TimeSpan PositionRefreshInterval = TimeSpan.FromMilliseconds(200);
    private IReadOnlyDictionary<AxisAddress, AxisConfig> _axes =
        new Dictionary<AxisAddress, AxisConfig>();
    private CancellationTokenSource? _refreshCancellation;
    private List<PositionPoint> _allPoints = [];
    private AxisGroupOptionViewModel? _selectedGroup;
    private PointRowViewModel? _selectedPoint;
    private JogDirectionViewModel? _activeDirection;
    private AxisControlCardViewModel? _xAxisCard;
    private AxisControlCardViewModel? _yAxisCard;
    private AxisControlCardViewModel? _zAxisCard;
    private AxisControlCardViewModel? _rAxisCard;
    private PointDebugJogPadViewModel _jogPad = new([]);
    private bool _activeContinuousJog;
    private bool _axisMotionPending;
    private DateTimeOffset _axisMoveEarliestCompletion;
    private DateTimeOffset _axisMoveStarted;
    private double _jogSpeed = 10;
    private string _statusMessage = "准备就绪";
    private MotionStatusLevel _statusLevel = MotionStatusLevel.Neutral;
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
        AddPointCommand = new DelegateCommand(
            () => AddPoint(),
            () => IsInteractionEnabled && SelectedGroup is not null);
        SaveCurrentPositionCommand = new AsyncDelegateCommand(
            SaveCurrentPositionAsync,
            () => IsInteractionEnabled && SelectedGroup is not null);
        BeginEditPointCommand = new DelegateCommand<PointRowViewModel>(
            BeginEditPoint,
            row => row is { IsEditing: false } && IsInteractionEnabled);
        SavePointCommand = new AsyncDelegateCommand<PointRowViewModel>(
            SavePointAsync,
            row => row is { IsEditing: true } && IsInteractionEnabled);
        CancelEditPointCommand = new DelegateCommand<PointRowViewModel>(CancelEditPoint);
        DeletePointCommand = new AsyncDelegateCommand<PointRowViewModel>(
            DeletePointAsync,
            row => row is { IsEditing: false } && IsInteractionEnabled);
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
    public ObservableCollection<AxisControlCardViewModel> AxisCards { get; } = [];
    public ObservableCollection<PointRowViewModel> Points { get; } = [];
    public ObservableCollection<JogModuleViewModel> Modules { get; } = [];
    public ObservableCollection<JogModuleViewModel> CenterModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> LinearModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> RotaryModules { get; } = [];
    public ObservableCollection<JogModuleViewModel> AuxiliaryModules { get; } = [];

    public AsyncDelegateCommand LoadCommand { get; }
    public AsyncDelegateCommand RefreshPositionsCommand { get; }
    public DelegateCommand AddPointCommand { get; }
    public AsyncDelegateCommand SaveCurrentPositionCommand { get; }
    public DelegateCommand<PointRowViewModel> BeginEditPointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> SavePointCommand { get; }
    public DelegateCommand<PointRowViewModel> CancelEditPointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> DeletePointCommand { get; }
    public AsyncDelegateCommand<PointRowViewModel> LocatePointCommand { get; }
    public AsyncDelegateCommand<JogDirectionViewModel> StartJogCommand { get; }
    public AsyncDelegateCommand<JogDirectionViewModel> StopJogCommand { get; }
    public AsyncDelegateCommand StopGroupCommand { get; }

    public PointRowViewModel? SelectedPoint
    {
        get => _selectedPoint;
        set
        {
            if (SetProperty(ref _selectedPoint, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public AxisControlCardViewModel? XAxisCard
    {
        get => _xAxisCard;
        private set => SetProperty(ref _xAxisCard, value);
    }

    public AxisControlCardViewModel? YAxisCard
    {
        get => _yAxisCard;
        private set => SetProperty(ref _yAxisCard, value);
    }

    public AxisControlCardViewModel? ZAxisCard
    {
        get => _zAxisCard;
        private set => SetProperty(ref _zAxisCard, value);
    }

    public AxisControlCardViewModel? RAxisCard
    {
        get => _rAxisCard;
        private set => SetProperty(ref _rAxisCard, value);
    }

    public PointDebugJogPadViewModel JogPad
    {
        get => _jogPad;
        private set => SetProperty(ref _jogPad, value);
    }

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

    public MotionStatusLevel StatusLevel
    {
        get => _statusLevel;
        private set => SetProperty(ref _statusLevel, value);
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
        SetStatus("正在加载点位与轴分组...", MotionStatusLevel.Warning);
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
                SetStatus("没有可用分组，请先创建轴分组。", MotionStatusLevel.Neutral);
            }
            else
            {
                SetStatus(
                    $"已加载 {Points.Count} 个点位。",
                    _motionExecution.IsMotionAvailable
                        ? MotionStatusLevel.Success
                        : MotionStatusLevel.Neutral);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ClearSelectedGroup();
            SetStatus(
                $"加载点位调试页面失败：{exception.Message}",
                MotionStatusLevel.Error);
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

            foreach (var card in AxisCards)
            {
                if (byAddress.TryGetValue(card.Address, out var state))
                {
                    card.ApplyState(state);
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

            if (_axisMotionPending &&
                DateTimeOffset.UtcNow >= _axisMoveEarliestCompletion &&
                _activeTargets.Count == 1)
            {
                var target = _activeTargets.Single();
                if (byAddress.TryGetValue(target.Key, out var state) &&
                    _axes.TryGetValue(target.Key, out var axis))
                {
                    var tolerance = Math.Max(0, axis.InPositionError);
                    if (!state.IsMoving &&
                        Math.Abs(state.ActualPosition - target.Value) <= tolerance)
                    {
                        CompleteAxisMove("单轴运动完成。");
                        return;
                    }

                    if (DateTimeOffset.UtcNow - _axisMoveStarted >=
                        TimeSpan.FromSeconds(Math.Max(0.1, axis.InPositionTimeout)))
                    {
                        await EmergencyStopForFaultAsync("单轴运动超时", cancellationToken);
                        return;
                    }
                }
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
                    SetStatus("点位定位完成。", MotionStatusLevel.Success);
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
                SetStatus(
                    $"读取实时位置失败：{exception.Message}",
                    MotionStatusLevel.Error);
            }
        }
    }

    public async Task StartJogAsync(
        JogDirectionViewModel direction,
        CancellationToken cancellationToken = default)
    {
        if (!IsInteractionEnabled ||
            !_axes.TryGetValue(direction.Address, out var axis) ||
            AxisCards.FirstOrDefault(card => card.Address == direction.Address) is not { } card)
        {
            return;
        }

        _activeDirection = direction;
        direction.IsActive = true;
        IsMotionActive = true;
        try
        {
            switch (card.SelectedMode)
            {
                case AxisMotionMode.Continuous:
                    _activeContinuousJog = true;
                    SetStatus(
                        $"Jog：{direction.AxisName} {direction.Label}",
                        MotionStatusLevel.Warning);
                    await _motionExecution.StartJogAsync(
                        axis,
                        direction.Direction,
                        card.JogSpeed,
                        cancellationToken);
                    break;

                case AxisMotionMode.Relative:
                    _activeContinuousJog = false;
                    var delta = Math.Abs(card.RelativeDistance) * direction.Direction;
                    var relativeTarget = await _motionExecution.MoveAxisRelativeAsync(
                        axis,
                        delta,
                        card.MotionSpeed,
                        cancellationToken);
                    BeginAxisMove(direction.Address, relativeTarget);
                    SetStatus(
                        $"{direction.AxisName} 相对运动已下发。",
                        MotionStatusLevel.Warning);
                    break;

                case AxisMotionMode.Absolute:
                    _activeContinuousJog = false;
                    var absoluteTarget = Math.Abs(card.TargetPosition) * direction.Direction;
                    await _motionExecution.MoveAxisAbsoluteAsync(
                        axis,
                        absoluteTarget,
                        card.MotionSpeed,
                        cancellationToken);
                    BeginAxisMove(direction.Address, absoluteTarget);
                    SetStatus(
                        $"{direction.AxisName} 绝对运动已下发。",
                        MotionStatusLevel.Warning);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (ReferenceEquals(_activeDirection, direction))
            {
                direction.IsActive = false;
                _activeDirection = null;
                _activeContinuousJog = false;
                _axisMotionPending = false;
                IsMotionActive = false;
            }

            SetStatus($"运动启动失败：{exception.Message}", MotionStatusLevel.Error);
        }
    }

    public async Task StopJogAsync(
        JogDirectionViewModel direction,
        CancellationToken cancellationToken = default)
    {
        if (!_activeContinuousJog ||
            _activeDirection is null ||
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
            SetStatus($"{direction.AxisName} 已停止。", MotionStatusLevel.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus($"停止失败：{exception.Message}", MotionStatusLevel.Error);
        }
        if (stopped)
        {
            direction.IsActive = false;
            _activeDirection = null;
            _activeContinuousJog = false;
            IsMotionActive = false;
        }
    }

    public PointRowViewModel AddPoint()
    {
        var group = SelectedGroup?.Config
            ?? throw new InvalidOperationException("请先选择分组。");
        var positions = group.Members.Select(member => new PointAxisPosition
        {
            Address = member.Address,
            Position = 0
        });
        return AddPoint(
            group,
            positions,
            "已新增点位，请填写名称、速度和位置后保存。");
    }

    public async Task SaveCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        var group = SelectedGroup?.Config;
        if (group is null)
        {
            SetStatus("请先选择分组。", MotionStatusLevel.Neutral);
            return;
        }

        var configs = SelectedAxisConfigs();
        if (configs.Count == 0)
        {
            SetStatus("当前分组没有可读取的轴。", MotionStatusLevel.Neutral);
            return;
        }

        IsBusy = true;
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

            var row = AddPoint(
                group,
                group.Members.Select(member => new PointAxisPosition
                {
                    Address = member.Address,
                    Position = byAddress[member.Address].ActualPosition
                }),
                "正在保存当前位置...");
            await SavePointAsync(row, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus(
                $"保存当前位置失败：{exception.Message}",
                MotionStatusLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PointRowViewModel AddPoint(
        AxisGroupConfig group,
        IEnumerable<PointAxisPosition> positions,
        string statusMessage)
    {
        var name = NextPointName(group.Id);
        var point = new PositionPoint
        {
            Id = Guid.NewGuid().ToString("N"),
            GroupId = group.Id,
            Name = name,
            Speed = 50,
            AxisPositions = positions.Select(position => position with { }).ToList()
        };
        var row = CreatePointRow(point, group, isNew: true);
        Points.Add(row);
        SelectedPoint = row;
        RaisePropertyChanged(nameof(HasPoints));
        SetStatus(statusMessage, MotionStatusLevel.Info);
        return row;
    }

    public void BeginEditPoint(PointRowViewModel row)
    {
        SelectedPoint = row;
        row.BeginEdit();
        RaiseCommandStates();
    }

    public async Task SavePointAsync(
        PointRowViewModel row,
        CancellationToken cancellationToken = default)
    {
        var error = ValidatePoint(row);
        if (error is not null)
        {
            SetStatus(error, MotionStatusLevel.Warning);
            return;
        }

        await _pointPersistenceGate.WaitAsync(cancellationToken);
        try
        {
            var currentError = ValidatePoint(row);
            if (currentError is not null)
            {
                SetStatus(currentError, MotionStatusLevel.Warning);
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
            SelectedPoint = row;
            RaiseCommandStates();
            SetStatus($"点位“{row.Name}”已保存。", MotionStatusLevel.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus($"保存点位失败：{exception.Message}", MotionStatusLevel.Error);
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
            if (ReferenceEquals(SelectedPoint, row))
            {
                SelectedPoint = null;
            }
            RaisePropertyChanged(nameof(HasPoints));
        }
        else
        {
            row.CancelEdit();
        }

        RaiseCommandStates();
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
            if (ReferenceEquals(SelectedPoint, row))
            {
                SelectedPoint = Points.FirstOrDefault();
            }
            RaisePropertyChanged(nameof(HasPoints));
            SetStatus($"点位“{row.Name}”已删除。", MotionStatusLevel.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus($"删除点位失败：{exception.Message}", MotionStatusLevel.Error);
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
            SetStatus(
                $"点位“{row.Name}”分组已变更，无法定位。",
                MotionStatusLevel.Warning);
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
            SetStatus(
                $"已下发点位“{row.Name}”定位指令。",
                MotionStatusLevel.Warning);
        }
        catch (MotionFailStopException exception)
        {
            _pointMotionPending = false;
            row.MarkFailed(exception.Message);
            SetStatus(exception.Message, MotionStatusLevel.Error);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _pointMotionPending = false;
            IsMotionActive = false;
            row.MarkFailed(exception.Message);
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            SetStatus($"定位失败：{exception.Message}", MotionStatusLevel.Error);
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
            SetStatus("当前分组已停止。", MotionStatusLevel.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus($"停止失败：{exception.Message}", MotionStatusLevel.Error);
        }
        if (stopped)
        {
            _pointMotionPending = false;
            _axisMotionPending = false;
            _activePoint?.MarkStopped();
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            if (_activeDirection is not null)
            {
                _activeDirection.IsActive = false;
                _activeDirection = null;
            }

            _activeContinuousJog = false;
            IsMotionActive = false;
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext) => StartRefreshLoop();
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        StopRefreshLoop();
        _ = StopGroupAsync();
    }

    private void StartRefreshLoop()
    {
        StopRefreshLoop();
        _refreshCancellation = new CancellationTokenSource();
        _ = RunRefreshLoopAsync(_refreshCancellation.Token);
    }

    private void StopRefreshLoop()
    {
        var cancellation = _refreshCancellation;
        _refreshCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync(cancellationToken);
            using var timer = new PeriodicTimer(PositionRefreshInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshPositionsAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

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
            var card = new AxisControlCardViewModel(
                axis,
                member.Role,
                RoleLabel(member, axis),
                DisplayUnit(axis.Unit),
                (enabled, cancellationToken) => SetAxisServoEnabledAsync(
                    axis,
                    enabled,
                    cancellationToken));
            AxisCards.Add(card);
            AssignAxisCard(card);
        }

        var build = _jogModuleFactory.Build(group, _axes);
        foreach (var module in build.Modules)
        {
            Modules.Add(module);
            RegionCollection(module.Region).Add(module);
        }
        JogPad = new PointDebugJogPadViewModel(
            build.Modules.SelectMany(module => module.Directions));

        foreach (var point in _allPoints.Where(point =>
                     string.Equals(point.GroupId, group.Id, StringComparison.Ordinal)))
        {
            Points.Add(CreatePointRow(point, group));
        }

        SelectedPoint = Points.FirstOrDefault();

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
        AxisCards.Clear();
        Points.Clear();
        Modules.Clear();
        CenterModules.Clear();
        LinearModules.Clear();
        RotaryModules.Clear();
        AuxiliaryModules.Clear();
        XAxisCard = null;
        YAxisCard = null;
        ZAxisCard = null;
        RAxisCard = null;
        JogPad = new PointDebugJogPadViewModel([]);
        SelectedPoint = null;
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

    private void AssignAxisCard(AxisControlCardViewModel card)
    {
        switch (card.Role)
        {
            case AxisRole.X:
            case AxisRole.XY:
                XAxisCard ??= card;
                break;
            case AxisRole.Y:
                YAxisCard ??= card;
                break;
            case AxisRole.Z:
            case AxisRole.V:
            case AxisRole.W:
                ZAxisCard ??= card;
                break;
            case AxisRole.R:
            case AxisRole.U:
                RAxisCard ??= card;
                break;
            default:
                XAxisCard ??= card;
                break;
        }
    }

    private async Task SetAxisServoEnabledAsync(
        AxisConfig axis,
        bool enabled,
        CancellationToken cancellationToken)
    {
        try
        {
            await _motionExecution.SetServoEnabledAsync(
                axis,
                enabled,
                cancellationToken);
            SetStatus(
                $"轴 {axis.AxisName} 已{(enabled ? "使能" : "去使能")}。",
                MotionStatusLevel.Info);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetStatus(
                $"轴 {axis.AxisName} {(enabled ? "使能" : "去使能")}失败：{exception.Message}",
                MotionStatusLevel.Error);
            throw;
        }
    }

    private void BeginAxisMove(AxisAddress address, double target)
    {
        _axisMotionPending = true;
        _axisMoveEarliestCompletion = DateTimeOffset.UtcNow.AddMilliseconds(250);
        _axisMoveStarted = DateTimeOffset.UtcNow;
        _activeTargets = new Dictionary<AxisAddress, double> { [address] = target };
    }

    private void CompleteAxisMove(string message)
    {
        _axisMotionPending = false;
        _activeTargets = new Dictionary<AxisAddress, double>();
        if (_activeDirection is not null)
        {
            _activeDirection.IsActive = false;
            _activeDirection = null;
        }

        _activeContinuousJog = false;
        IsMotionActive = false;
        SetStatus(message, MotionStatusLevel.Success);
    }

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
        AddPointCommand.RaiseCanExecuteChanged();
        SaveCurrentPositionCommand.RaiseCanExecuteChanged();
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
            SetStatus(reason, MotionStatusLevel.Error);
            return;
        }

        try
        {
            await _motionExecution.StopAsync(
                addresses,
                MotionStopMode.Emergency,
                cancellationToken);
            _pointMotionPending = false;
            _axisMotionPending = false;
            _activePoint?.MarkFailed(reason);
            _activePoint = null;
            _activeTargets = new Dictionary<AxisAddress, double>();
            if (_activeDirection is not null)
            {
                _activeDirection.IsActive = false;
                _activeDirection = null;
            }

            _activeContinuousJog = false;
            IsMotionActive = false;
            SetStatus(
                $"{reason}，已紧急停止当前分组。",
                MotionStatusLevel.Error);
        }
        catch (Exception stopException) when (stopException is not OperationCanceledException)
        {
            SetStatus(
                $"{reason}，且停止失败：{stopException.Message}",
                MotionStatusLevel.Error);
        }
    }

    private void SetStatus(string message, MotionStatusLevel level)
    {
        StatusLevel = level;
        StatusMessage = message;
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
