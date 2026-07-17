using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class AxisConfigViewModel : BindableBase
{
    private readonly IMotionConfigurationService _configurationService;
    private readonly IAxisConfigurationValidator _validator;
    private readonly IConfigurationFileDialogService _fileDialogs;
    private readonly IConfigurationPromptService _prompts;
    private AxisConfig[] _snapshot = [];
    private AxisConfigEditorViewModel? _selectedAxis;
    private string _searchText = string.Empty;
    private string _statusMessage = "准备就绪";
    private bool _isBusy;
    private bool _hasStructuralChanges;

    public AxisConfigViewModel(
        IMotionConfigurationService configurationService,
        IAxisConfigurationValidator validator,
        IConfigurationFileDialogService fileDialogs,
        IConfigurationPromptService prompts)
    {
        _configurationService = configurationService;
        _validator = validator;
        _fileDialogs = fileDialogs;
        _prompts = prompts;

        AxesView = CollectionViewSource.GetDefaultView(Axes);
        AxesView.Filter = MatchesSearch;

        LoadCommand = new AsyncDelegateCommand(LoadAsync);
        SaveCommand = new AsyncDelegateCommand(SaveAsync);
        ImportCommand = new AsyncDelegateCommand(ImportAsync);
        ExportCommand = new AsyncDelegateCommand(ExportAsync);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        AddAxisCommand = new AsyncDelegateCommand(AddAxisAsync);
        DeleteAxisCommand = new AsyncDelegateCommand(DeleteAxisAsync);
    }

    public ObservableCollection<AxisConfigEditorViewModel> Axes { get; } = [];
    public ICollectionView AxesView { get; }
    public IReadOnlyList<string> AxisTypes { get; } = ["DS402", "Pulse", "Virtual"];
    public IReadOnlyList<NamedValue<int>> JogDirections { get; } =
        [new("正向", 1), new("反向", -1)];
    public IReadOnlyList<string> HomeModes { get; } = ["ORG_P", "ORG_N", "LIMIT_P", "LIMIT_N"];

    public AxisConfigEditorViewModel? SelectedAxis
    {
        get => _selectedAxis;
        set
        {
            if (SetProperty(ref _selectedAxis, value))
            {
                RaisePropertyChanged(nameof(HasSelection));
            }
        }
    }

    public bool HasSelection => SelectedAxis is not null;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                AxesView.Refresh();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool HasUnsavedChanges => _hasStructuralChanges || Axes.Any(axis => axis.IsDirty);
    public bool HasValidationErrors => !_validator.ValidateCollection(CurrentModels()).IsValid;

    public AsyncDelegateCommand LoadCommand { get; }
    public AsyncDelegateCommand SaveCommand { get; }
    public AsyncDelegateCommand ImportCommand { get; }
    public AsyncDelegateCommand ExportCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand AddAxisCommand { get; }
    public AsyncDelegateCommand DeleteAxisCommand { get; }
    internal event Action<AxisConfigEditorViewModel>? AxisAdded;

    private Task LoadAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        if (!await CanReplaceCurrentAsync(cancellationToken))
        {
            return;
        }

        var loaded = await _configurationService.LoadAsync(cancellationToken);
        ReplaceAxes(loaded, createSnapshot: true);
        StatusMessage = $"已加载 {Axes.Count} 个轴配置";
    });

    private Task SaveAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(
        () => SaveCoreAsync(cancellationToken));

    private Task ImportAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        if (!await CanReplaceCurrentAsync(cancellationToken))
        {
            return;
        }

        var path = await _fileDialogs.SelectImportFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "已取消导入";
            return;
        }

        var imported = await _configurationService.ImportAsync(path, cancellationToken);
        var validation = _validator.ValidateCollection(imported);
        if (!validation.IsValid)
        {
            StatusMessage = string.Join("；", validation.Errors.Select(issue => issue.Message));
            return;
        }

        ReplaceAxes(imported, createSnapshot: false);
        _hasStructuralChanges = true;
        RaiseStateProperties();
        StatusMessage = $"已导入 {Axes.Count} 个轴配置，保存后写入默认配置";
    });

    private Task ExportAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        var validation = _validator.ValidateCollection(CurrentModels());
        if (!validation.IsValid)
        {
            StatusMessage = string.Join("；", validation.Errors.Select(issue => issue.Message));
            RaiseStateProperties();
            return;
        }

        var path = await _fileDialogs.SelectExportFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "已取消导出";
            return;
        }

        await _configurationService.ExportAsync(CurrentModels(), path, cancellationToken);
        StatusMessage = $"配置已导出到 {path}";
    });

    private Task RefreshAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        if (!await CanReplaceCurrentAsync(cancellationToken))
        {
            return;
        }

        ReplaceAxes(_snapshot, createSnapshot: false);
        _hasStructuralChanges = false;
        RaiseStateProperties();
        StatusMessage = "已恢复到最近一次加载或保存的配置";
    });

    private Task AddAxisAsync(CancellationToken _) => ExecuteBusyAsync(() =>
    {
        SearchText = string.Empty;
        var address = NextAvailableAddress(0, 0);
        var editor = CreateEditor(AxisConfig.CreateDefault(address, $"Axis{address.AxisNo}"));
        Axes.Add(editor);
        SelectedAxis = editor;
        MarkStructureChanged();
        StatusMessage = $"已添加轴 {editor.CardNo}/{editor.AxisNo}，请保存以持久化";
        AxisAdded?.Invoke(editor);
        return Task.CompletedTask;
    });

    private Task DeleteAxisAsync(CancellationToken cancellationToken) => ExecuteBusyAsync(async () =>
    {
        if (SelectedAxis is null || !await _prompts.ConfirmDeleteAsync(SelectedAxis.ToModel()))
        {
            return;
        }

        var index = Axes.IndexOf(SelectedAxis);
        DetachEditor(SelectedAxis);
        Axes.RemoveAt(index);
        SelectedAxis = Axes.ElementAtOrDefault(Math.Min(index, Axes.Count - 1));
        MarkStructureChanged();
        StatusMessage = "轴配置已从当前列表删除";
    });

    private async Task<bool> CanReplaceCurrentAsync(CancellationToken cancellationToken)
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        return await _prompts.ConfirmUnsavedChangesAsync() switch
        {
            ConfigurationPromptResult.DiscardAndContinue => true,
            ConfigurationPromptResult.SaveAndContinue => await SaveCoreAsync(cancellationToken),
            _ => false
        };
    }

    private async Task<bool> SaveCoreAsync(CancellationToken cancellationToken)
    {
        var models = CurrentModels();
        var validation = _validator.ValidateCollection(models);
        if (!validation.IsValid)
        {
            StatusMessage = string.Join("；", validation.Errors.Select(issue => issue.Message));
            RaiseStateProperties();
            return false;
        }

        await _configurationService.SaveAsync(models, cancellationToken);
        foreach (var axis in Axes)
        {
            axis.AcceptChanges();
        }

        _snapshot = models.Select(axis => axis with { }).ToArray();
        _hasStructuralChanges = false;
        RaiseStateProperties();
        StatusMessage = $"已保存 {Axes.Count} 个轴配置";
        return true;
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReplaceAxes(IEnumerable<AxisConfig> configs, bool createSnapshot)
    {
        var values = configs.Select(axis => axis with { }).ToArray();
        var selectedAddress = SelectedAxis?.Address;
        foreach (var axis in Axes)
        {
            DetachEditor(axis);
        }

        Axes.Clear();
        foreach (var config in values)
        {
            Axes.Add(CreateEditor(config));
        }

        SelectedAxis = selectedAddress is null
            ? Axes.FirstOrDefault()
            : Axes.FirstOrDefault(axis => axis.Address == selectedAddress) ?? Axes.FirstOrDefault();
        if (createSnapshot)
        {
            _snapshot = values.Select(axis => axis with { }).ToArray();
        }

        _hasStructuralChanges = false;
        AxesView.Refresh();
        RaiseStateProperties();
    }

    private AxisConfigEditorViewModel CreateEditor(AxisConfig config)
    {
        var editor = new AxisConfigEditorViewModel(config, _validator);
        editor.PropertyChanged += OnEditorPropertyChanged;
        editor.ErrorsChanged += OnEditorErrorsChanged;
        return editor;
    }

    private void DetachEditor(AxisConfigEditorViewModel editor)
    {
        editor.PropertyChanged -= OnEditorPropertyChanged;
        editor.ErrorsChanged -= OnEditorErrorsChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(AxisConfigEditorViewModel.AxisName)
            or nameof(AxisConfigEditorViewModel.AxisType)
            or nameof(AxisConfigEditorViewModel.CardNo)
            or nameof(AxisConfigEditorViewModel.AxisNo))
        {
            AxesView.Refresh();
        }

        RaiseStateProperties();
    }

    private void OnEditorErrorsChanged(object? sender, DataErrorsChangedEventArgs args) =>
        RaiseStateProperties();

    private AxisConfig[] CurrentModels() => Axes.Select(axis => axis.ToModel()).ToArray();

    private AxisAddress NextAvailableAddress(int cardNo, int startAxisNo)
    {
        var used = Axes.Select(axis => axis.Address).ToHashSet();
        var axisNo = Math.Max(0, startAxisNo);
        while (used.Contains(new AxisAddress(cardNo, axisNo)))
        {
            axisNo++;
        }

        return new AxisAddress(cardNo, axisNo);
    }

    private bool MatchesSearch(object item)
    {
        if (item is not AxisConfigEditorViewModel axis || string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var query = SearchText.Trim();
        return axis.AxisName.Contains(query, StringComparison.OrdinalIgnoreCase)
               || axis.AxisType.Contains(query, StringComparison.OrdinalIgnoreCase)
               || axis.CardNo.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
               || axis.AxisNo.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
               || $"{axis.CardNo}/{axis.AxisNo}".Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void MarkStructureChanged()
    {
        _hasStructuralChanges = true;
        AxesView.Refresh();
        RaiseStateProperties();
    }

    private void RaiseStateProperties()
    {
        RaisePropertyChanged(nameof(HasUnsavedChanges));
        RaisePropertyChanged(nameof(HasValidationErrors));
    }
}

public sealed record NamedValue<T>(string Name, T Value);
