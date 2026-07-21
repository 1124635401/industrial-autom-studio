using System.Collections.ObjectModel;
using System.Windows.Threading;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Repositories.Interfaces;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class IoMonitorViewModel : BindableBase, INavigationAware
{
    public const int PageSize = 24;

    private readonly IIoMonitorService _monitorService;
    private readonly IIoDisplayNameRepository _displayNameRepository;
    private readonly IMotionCardService _cardService;
    private readonly Dispatcher _dispatcher;
    private Dictionary<string, string> _displayNames = new(StringComparer.Ordinal);
    private IDisposable? _monitorLease;
    private CancellationTokenSource? _navigationCancellation;
    private bool _active;
    private int _diCurrentPage = 1;
    private int _doCurrentPage = 1;
    private int _diTotalPages = 1;
    private int _doTotalPages = 1;
    private string _statusMessage = "等待 IO 数据";

    public IoMonitorViewModel(
        IIoMonitorService monitorService,
        IIoDisplayNameRepository displayNameRepository,
        IMotionCardService cardService)
    {
        _monitorService = monitorService;
        _displayNameRepository = displayNameRepository;
        _cardService = cardService;
        _dispatcher = Dispatcher.CurrentDispatcher;

        DiPreviousPageCommand = new DelegateCommand(
            () => SetDiPage(DiCurrentPage - 1),
            () => DiCurrentPage > 1);
        DiNextPageCommand = new DelegateCommand(
            () => SetDiPage(DiCurrentPage + 1),
            () => DiCurrentPage < DiTotalPages);
        DiSelectPageCommand = new DelegateCommand<IoPageToken>(
            token => SetDiPage(token.PageNumber ?? DiCurrentPage),
            token => token is { PageNumber: not null });

        DoPreviousPageCommand = new DelegateCommand(
            () => SetDoPage(DoCurrentPage - 1),
            () => DoCurrentPage > 1);
        DoNextPageCommand = new DelegateCommand(
            () => SetDoPage(DoCurrentPage + 1),
            () => DoCurrentPage < DoTotalPages);
        DoSelectPageCommand = new DelegateCommand<IoPageToken>(
            token => SetDoPage(token.PageNumber ?? DoCurrentPage),
            token => token is { PageNumber: not null });

        BeginEditNameCommand = new DelegateCommand<IoPointItem>(BeginEditName);
        SaveNameCommand = new AsyncDelegateCommand<IoPointItem>(SaveNameAsync);
        CancelEditNameCommand = new DelegateCommand<IoPointItem>(CancelEditName);
        ToggleDoCommand = new AsyncDelegateCommand<IoPointItem>(
            ToggleDoAsync,
            point => point is { CanOperate: true, IsOn: not null, IsWriting: false });

        RefreshPaging();
    }

    public ObservableCollection<IoPointItem> AllDiPoints { get; } = [];
    public ObservableCollection<IoPointItem> AllDoPoints { get; } = [];
    public ObservableCollection<IoPointItem> PagedDiPoints { get; } = [];
    public ObservableCollection<IoPointItem> PagedDoPoints { get; } = [];
    public ObservableCollection<IoPageToken> DiPageTokens { get; } = [];
    public ObservableCollection<IoPageToken> DoPageTokens { get; } = [];

    public int DiCurrentPage
    {
        get => _diCurrentPage;
        private set => SetProperty(ref _diCurrentPage, value);
    }

    public int DoCurrentPage
    {
        get => _doCurrentPage;
        private set => SetProperty(ref _doCurrentPage, value);
    }

    public int DiTotalPages
    {
        get => _diTotalPages;
        private set => SetProperty(ref _diTotalPages, value);
    }

    public int DoTotalPages
    {
        get => _doTotalPages;
        private set => SetProperty(ref _doTotalPages, value);
    }

    public int DiTotalCount => AllDiPoints.Count;
    public int DoTotalCount => AllDoPoints.Count;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DelegateCommand DiPreviousPageCommand { get; }
    public DelegateCommand DiNextPageCommand { get; }
    public DelegateCommand<IoPageToken> DiSelectPageCommand { get; }
    public DelegateCommand DoPreviousPageCommand { get; }
    public DelegateCommand DoNextPageCommand { get; }
    public DelegateCommand<IoPageToken> DoSelectPageCommand { get; }
    public DelegateCommand<IoPointItem> BeginEditNameCommand { get; }
    public AsyncDelegateCommand<IoPointItem> SaveNameCommand { get; }
    public DelegateCommand<IoPointItem> CancelEditNameCommand { get; }
    public AsyncDelegateCommand<IoPointItem> ToggleDoCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();
        var names = await _displayNameRepository.LoadAsync(cancellationToken)
            .ConfigureAwait(false);
        _displayNames = new Dictionary<string, string>(names, StringComparer.Ordinal);
        await RunOnUiAsync(ApplyLoadedNames);
        _ = await _monitorService.ReadNowAsync(cancellationToken).ConfigureAwait(false);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _navigationCancellation?.Cancel();
        _navigationCancellation?.Dispose();
        _navigationCancellation = new CancellationTokenSource();
        _ = InitializeForNavigationAsync(_navigationCancellation.Token);
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _navigationCancellation?.Cancel();
        _navigationCancellation?.Dispose();
        _navigationCancellation = null;
        Deactivate();
    }

    private async Task InitializeForNavigationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await RunOnUiAsync(() => StatusMessage = exception.Message);
        }
    }

    private void EnsureActive()
    {
        if (_active)
        {
            return;
        }

        _monitorService.SnapshotChanged += OnSnapshotChanged;
        _monitorService.MonitorError += OnMonitorError;
        _cardService.ConnectionChanged += OnConnectionChanged;
        _monitorLease = _monitorService.AcquireMonitoring();
        _active = true;
    }

    private void Deactivate()
    {
        if (!_active)
        {
            return;
        }

        _monitorService.SnapshotChanged -= OnSnapshotChanged;
        _monitorService.MonitorError -= OnMonitorError;
        _cardService.ConnectionChanged -= OnConnectionChanged;
        _monitorLease?.Dispose();
        _monitorLease = null;
        _active = false;
    }

    private void OnSnapshotChanged(object? sender, IoSnapshot snapshot) =>
        RunOnUi(() => ApplySnapshot(snapshot));

    private void OnMonitorError(object? sender, IoMonitorError error) =>
        RunOnUi(() => StatusMessage = error.Message);

    private void OnConnectionChanged(object? sender, bool connected) =>
        RunOnUi(UpdateCanOperate);

    private void ApplySnapshot(IoSnapshot snapshot)
    {
        var diStructureChanged = SynchronizePoints(
            AllDiPoints,
            snapshot.DigitalInputs,
            IoPointType.DI);
        var doStructureChanged = SynchronizePoints(
            AllDoPoints,
            snapshot.DigitalOutputs,
            IoPointType.DO);
        StatusMessage = _cardService.IsConnected ? "IO 状态已更新" : "控制卡未连接";
        if (diStructureChanged || doStructureChanged)
        {
            RefreshPaging();
        }

        UpdateCanOperate();
    }

    private bool SynchronizePoints(
        ObservableCollection<IoPointItem> points,
        IReadOnlyList<bool?> values,
        IoPointType type)
    {
        var structureChanged = points.Count != values.Count;

        while (points.Count > values.Count)
        {
            points.RemoveAt(points.Count - 1);
        }

        while (points.Count < values.Count)
        {
            var index = points.Count + 1;
            var address = $"{type}_{index}";
            _displayNames.TryGetValue(address, out var displayName);
            points.Add(new IoPointItem(index, type, displayName));
        }

        for (var index = 0; index < values.Count; index++)
        {
            points[index].IsOn = values[index];
        }

        return structureChanged;
    }

    private void ApplyLoadedNames()
    {
        foreach (var point in AllDiPoints.Concat(AllDoPoints))
        {
            point.RestoreDisplayName(
                _displayNames.TryGetValue(point.Address, out var name)
                    ? name
                    : point.Address);
        }
    }

    private void RefreshPaging()
    {
        DiTotalPages = CalculateTotalPages(AllDiPoints.Count);
        DoTotalPages = CalculateTotalPages(AllDoPoints.Count);
        DiCurrentPage = Math.Clamp(DiCurrentPage, 1, DiTotalPages);
        DoCurrentPage = Math.Clamp(DoCurrentPage, 1, DoTotalPages);

        ReplacePage(PagedDiPoints, AllDiPoints, DiCurrentPage);
        ReplacePage(PagedDoPoints, AllDoPoints, DoCurrentPage);
        ReplaceTokens(DiPageTokens, BuildPageTokens(DiCurrentPage, DiTotalPages));
        ReplaceTokens(DoPageTokens, BuildPageTokens(DoCurrentPage, DoTotalPages));

        RaisePropertyChanged(nameof(DiTotalCount));
        RaisePropertyChanged(nameof(DoTotalCount));
        RaisePagerCanExecuteChanged();
    }

    private void SetDiPage(int page)
    {
        var target = Math.Clamp(page, 1, DiTotalPages);
        if (target == DiCurrentPage)
        {
            return;
        }

        DiCurrentPage = target;
        ReplacePage(PagedDiPoints, AllDiPoints, target);
        ReplaceTokens(DiPageTokens, BuildPageTokens(target, DiTotalPages));
        RaisePagerCanExecuteChanged();
    }

    private void SetDoPage(int page)
    {
        var target = Math.Clamp(page, 1, DoTotalPages);
        if (target == DoCurrentPage)
        {
            return;
        }

        DoCurrentPage = target;
        ReplacePage(PagedDoPoints, AllDoPoints, target);
        ReplaceTokens(DoPageTokens, BuildPageTokens(target, DoTotalPages));
        RaisePagerCanExecuteChanged();
    }

    private void BeginEditName(IoPointItem point)
    {
        point.BeginEdit();
        SaveNameCommand.RaiseCanExecuteChanged();
    }

    private async Task SaveNameAsync(
        IoPointItem point,
        CancellationToken cancellationToken)
    {
        var previousName = point.DisplayName;
        point.CommitEdit();
        var names = BuildDisplayNameMap();
        try
        {
            await _displayNameRepository.SaveAsync(names, cancellationToken)
                .ConfigureAwait(false);
            _displayNames = names;
            await RunOnUiAsync(() => StatusMessage = $"已保存 {point.Address} 的显示名称");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RunOnUiAsync(() =>
            {
                point.RestoreDisplayName(previousName);
                StatusMessage = exception.Message;
            });
        }
        finally
        {
            SaveNameCommand.RaiseCanExecuteChanged();
        }
    }

    private void CancelEditName(IoPointItem point)
    {
        point.CancelEdit();
        SaveNameCommand.RaiseCanExecuteChanged();
    }

    private async Task ToggleDoAsync(
        IoPointItem point,
        CancellationToken cancellationToken)
    {
        if (point is not { Type: IoPointType.DO, CanOperate: true, IsOn: not null })
        {
            return;
        }

        var actualState = point.IsOn;
        point.IsWriting = true;
        UpdateCanOperate();
        try
        {
            await _monitorService.WriteDigitalOutputAsync(
                    point.Index,
                    !actualState.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RunOnUiAsync(() =>
            {
                point.IsOn = actualState;
                StatusMessage = exception.Message;
            });
        }
        finally
        {
            await RunOnUiAsync(() =>
            {
                point.IsWriting = false;
                UpdateCanOperate();
            });
        }
    }

    private Dictionary<string, string> BuildDisplayNameMap() =>
        AllDiPoints.Concat(AllDoPoints)
            .Where(point => !string.Equals(
                point.DisplayName,
                point.Address,
                StringComparison.Ordinal))
            .ToDictionary(
                point => point.Address,
                point => point.DisplayName,
                StringComparer.Ordinal);

    private void UpdateCanOperate()
    {
        foreach (var point in AllDiPoints)
        {
            point.CanOperate = false;
        }

        foreach (var point in AllDoPoints)
        {
            point.CanOperate = _cardService.IsConnected &&
                               _monitorService.CanWriteDigitalOutputs &&
                               point.IsOn.HasValue &&
                               !point.IsWriting;
        }

        ToggleDoCommand.RaiseCanExecuteChanged();
    }

    private static int CalculateTotalPages(int count) =>
        Math.Max(1, (count + PageSize - 1) / PageSize);

    private static void ReplacePage(
        ObservableCollection<IoPointItem> target,
        IReadOnlyList<IoPointItem> source,
        int page)
    {
        target.Clear();
        foreach (var point in source.Skip((page - 1) * PageSize).Take(PageSize))
        {
            target.Add(point);
        }
    }

    private static IReadOnlyList<IoPageToken> BuildPageTokens(
        int currentPage,
        int totalPages)
    {
        var pages = totalPages <= 5
            ? Enumerable.Range(1, totalPages).ToArray()
            : new[] { 1, currentPage - 1, currentPage, currentPage + 1, totalPages }
                .Where(page => page >= 1 && page <= totalPages)
                .Distinct()
                .Order()
                .ToArray();

        var tokens = new List<IoPageToken>();
        int? previous = null;
        foreach (var page in pages)
        {
            if (previous.HasValue && page - previous.Value > 1)
            {
                tokens.Add(new IoPageToken(null, "…", false));
            }

            tokens.Add(new IoPageToken(page, page.ToString(), page == currentPage));
            previous = page;
        }

        return tokens;
    }

    private static void ReplaceTokens(
        ObservableCollection<IoPageToken> target,
        IEnumerable<IoPageToken> tokens)
    {
        target.Clear();
        foreach (var token in tokens)
        {
            target.Add(token);
        }
    }

    private void RaisePagerCanExecuteChanged()
    {
        DiPreviousPageCommand.RaiseCanExecuteChanged();
        DiNextPageCommand.RaiseCanExecuteChanged();
        DiSelectPageCommand.RaiseCanExecuteChanged();
        DoPreviousPageCommand.RaiseCanExecuteChanged();
        DoNextPageCommand.RaiseCanExecuteChanged();
        DoSelectPageCommand.RaiseCanExecuteChanged();
    }

    private void RunOnUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _ = _dispatcher.InvokeAsync(action);
    }

    private Task RunOnUiAsync(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(action).Task;
    }
}
