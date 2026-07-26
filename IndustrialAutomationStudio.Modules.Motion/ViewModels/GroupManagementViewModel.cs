using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using IndustrialAutomationStudio.Modules.Motion.Models;
using IndustrialAutomationStudio.Modules.Motion.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace IndustrialAutomationStudio.Modules.Motion.ViewModels;

public sealed class GroupManagementViewModel : BindableBase, IConfirmNavigationRequest
{
    private readonly IAxisConfigService _axisConfigService;
    private readonly IAxisGroupConfigService _groupConfigService;
    private readonly IAxisGroupPromptService? _promptService;
    private readonly ObservableCollection<AxisAssignmentItemViewModel> _availableAxes = [];
    private readonly ObservableCollection<AxisAssignmentItemViewModel> _assignedAxes = [];
    private Dictionary<AxisAddress, AxisConfig> _axesByAddress = [];
    private AxisGroupItemViewModel? _selectedGroup;
    private string _editingGroupName = string.Empty;
    private string _availableAxisSearchText = string.Empty;
    private string _assignedAxisSearchText = string.Empty;
    private string _statusMessage = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isDirty;
    private bool _isBusy;
    private bool _hasLoadError;
    private bool _isLoadingEditor;
    private bool _isSettingSelection;

    internal GroupManagementViewModel(
        IAxisConfigService axisConfigService,
        IAxisGroupConfigService groupConfigService)
        : this(axisConfigService, groupConfigService, null)
    {
    }

    public GroupManagementViewModel(
        IAxisConfigService axisConfigService,
        IAxisGroupConfigService groupConfigService,
        IAxisGroupPromptService? promptService)
    {
        _axisConfigService = axisConfigService;
        _groupConfigService = groupConfigService;
        _promptService = promptService;
        AvailableAxesView = CollectionViewSource.GetDefaultView(_availableAxes);
        AvailableAxesView.Filter = FilterAvailableAxis;
        AssignedAxesView = CollectionViewSource.GetDefaultView(_assignedAxes);
        AssignedAxesView.Filter = FilterAssignedAxis;

        LoadCommand = new AsyncDelegateCommand(LoadAsync);
        AddSelectedAxesCommand = new DelegateCommand(
            () => MoveItems(_availableAxes.Where(item => item.IsSelected).ToArray(), true),
            () => !IsBusy && SelectedGroup is not null && _availableAxes.Any(item => item.IsSelected));
        AddAllFilteredAxesCommand = new DelegateCommand(
            () => MoveItems(AvailableAxesView.Cast<AxisAssignmentItemViewModel>().ToArray(), true),
            () => !IsBusy && SelectedGroup is not null && !AvailableAxesView.IsEmpty);
        RemoveSelectedAxesCommand = new DelegateCommand(
            () => MoveItems(_assignedAxes.Where(item => item.IsSelected).ToArray(), false),
            () => !IsBusy && SelectedGroup is not null && _assignedAxes.Any(item => item.IsSelected));
        RemoveAllFilteredAxesCommand = new DelegateCommand(
            () => MoveItems(AssignedAxesView.Cast<AxisAssignmentItemViewModel>().ToArray(), false),
            () => !IsBusy && SelectedGroup is not null && !AssignedAxesView.IsEmpty);
        NewGroupCommand = new AsyncDelegateCommand(NewGroupAsync, () => !IsBusy);
        SaveGroupCommand = new AsyncDelegateCommand(
            async () => { await SaveGroupAsync(); },
            () => !IsBusy && SelectedGroup is not null && IsDirty && ValidationMessage.Length == 0);
        DeleteGroupCommand = new AsyncDelegateCommand(
            async () => { await DeleteGroupAsync(); },
            () => !IsBusy && SelectedGroup is not null);
    }

    public ObservableCollection<AxisGroupItemViewModel> Groups { get; } = [];

    public ObservableCollection<AxisAssignmentItemViewModel> AvailableAxes => _availableAxes;

    public ObservableCollection<AxisAssignmentItemViewModel> AssignedAxes => _assignedAxes;

    public ICollectionView AvailableAxesView { get; }

    public ICollectionView AssignedAxesView { get; }

    public AxisGroupItemViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (_isSettingSelection)
            {
                SetSelectedGroup(value);
                return;
            }

            if (!ReferenceEquals(_selectedGroup, value))
            {
                _ = SelectGroupAsync(value);
            }
        }
    }

    public string EditingGroupName
    {
        get => _editingGroupName;
        set
        {
            if (SetProperty(ref _editingGroupName, value) && !_isLoadingEditor)
            {
                ValidateEditingName();
                RecalculateDirtyState();
            }
        }
    }

    public string AvailableAxisSearchText
    {
        get => _availableAxisSearchText;
        set
        {
            if (SetProperty(ref _availableAxisSearchText, value))
            {
                AvailableAxesView.Refresh();
                RaiseCommandStates();
            }
        }
    }

    public string AssignedAxisSearchText
    {
        get => _assignedAxisSearchText;
        set
        {
            if (SetProperty(ref _assignedAxisSearchText, value))
            {
                AssignedAxesView.Refresh();
                RaiseCommandStates();
            }
        }
    }

    public int SelectedAvailableCount => _availableAxes.Count(item => item.IsSelected);

    public int SelectedAssignedCount => _assignedAxes.Count(item => item.IsSelected);

    public int SelectedAxisCount => SelectedAvailableCount + SelectedAssignedCount;

    public int AssignedAxisCount => _assignedAxes.Count;

    public bool HasSelectedGroup => SelectedGroup is not null;

    public bool IsNotBusy => !IsBusy;

    public bool IsEditorEnabled => HasSelectedGroup && !IsBusy;

    public bool HasLoadError
    {
        get => _hasLoadError;
        private set => SetProperty(ref _hasLoadError, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(IsNotBusy));
                RaisePropertyChanged(nameof(IsEditorEnabled));
                RaiseCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public AsyncDelegateCommand LoadCommand { get; }

    public DelegateCommand AddSelectedAxesCommand { get; }

    public DelegateCommand AddAllFilteredAxesCommand { get; }

    public DelegateCommand RemoveSelectedAxesCommand { get; }

    public DelegateCommand RemoveAllFilteredAxesCommand { get; }

    public AsyncDelegateCommand NewGroupCommand { get; }

    public AsyncDelegateCommand SaveGroupCommand { get; }

    public AsyncDelegateCommand DeleteGroupCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        HasLoadError = false;
        StatusMessage = string.Empty;
        try
        {
            var axesTask = _axisConfigService.LoadAsync();
            var groupsTask = _groupConfigService.LoadAsync();
            await Task.WhenAll(axesTask, groupsTask);

            _axesByAddress = axesTask.Result
                .GroupBy(axis => axis.Address)
                .ToDictionary(group => group.Key, group => group.First());
            Groups.Clear();
            var missingCount = 0;
            foreach (var group in groupsTask.Result)
            {
                var validCount = group.Members.Count(member =>
                    _axesByAddress.ContainsKey(member.Address));
                missingCount += group.Members.Count - validCount;
                Groups.Add(new AxisGroupItemViewModel(group, validCount));
            }

            SetSelectedGroup(Groups.FirstOrDefault());
            if (missingCount > 0)
            {
                StatusMessage = $"已忽略 {missingCount} 个失效轴关联。";
            }
            else if (Groups.Count > 0)
            {
                StatusMessage = $"已加载 {Groups.Count} 个分组。";
            }

            IsDirty = false;
        }
        catch (Exception exception)
        {
            Groups.Clear();
            SetSelectedGroup(null);
            HasLoadError = true;
            StatusMessage = $"加载分组配置失败：{exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> NewGroupAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        if (!await ResolveUnsavedChangesAsync())
        {
            return false;
        }

        var model = new AxisGroupConfig { Name = "新建分组" };
        var item = new AxisGroupItemViewModel(model, 0, isTransient: true);
        Groups.Add(item);
        SetSelectedGroup(item);
        IsDirty = true;
        ValidateEditingName();
        StatusMessage = "请输入分组名称并分配轴。";
        return true;
    }

    public async Task<bool> SaveGroupAsync()
    {
        if (SelectedGroup is null || IsBusy)
        {
            return false;
        }

        ValidateEditingName();
        if (ValidationMessage.Length > 0)
        {
            StatusMessage = ValidationMessage;
            return false;
        }

        return await SaveCurrentGroupAsync();
    }

    public async Task<bool> DeleteGroupAsync()
    {
        var deletingGroup = SelectedGroup;
        if (deletingGroup is null || IsBusy)
        {
            return false;
        }

        if (!await ResolveUnsavedChangesAsync())
        {
            return false;
        }

        if (!Groups.Contains(deletingGroup))
        {
            return true;
        }

        if (_promptService is not null
            && !await _promptService.ConfirmDeleteAsync(deletingGroup.Name))
        {
            return false;
        }

        var deletingIndex = Groups.IndexOf(deletingGroup);
        try
        {
            IsBusy = true;
            var remaining = Groups
                .Where(group => !ReferenceEquals(group, deletingGroup) && !group.IsTransient)
                .Select(group => Clone(group.Snapshot))
                .ToArray();
            await _groupConfigService.SaveAsync(remaining);
            Groups.Remove(deletingGroup);
            var nextIndex = Math.Min(deletingIndex, Groups.Count - 1);
            SetSelectedGroup(nextIndex >= 0 ? Groups[nextIndex] : null);
            StatusMessage = $"已删除分组“{deletingGroup.Name}”。";
            return true;
        }
        catch (Exception exception)
        {
            StatusMessage = $"删除分组失败：{exception.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SelectGroupAsync(AxisGroupItemViewModel? target)
    {
        if (IsBusy)
        {
            RaisePropertyChanged(nameof(SelectedGroup));
            return false;
        }

        if (ReferenceEquals(target, SelectedGroup))
        {
            return true;
        }

        if (!await ResolveUnsavedChangesAsync())
        {
            RaisePropertyChanged(nameof(SelectedGroup));
            return false;
        }

        if (target is not null && !Groups.Contains(target))
        {
            return false;
        }

        SetSelectedGroup(target);
        return true;
    }

    public async void ConfirmNavigationRequest(
        NavigationContext navigationContext,
        Action<bool> continuationCallback)
    {
        ArgumentNullException.ThrowIfNull(continuationCallback);
        continuationCallback(await ResolveUnsavedChangesAsync());
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (!IsBusy && !IsDirty)
        {
            _ = LoadAsync();
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    private void LoadSelectedGroupEditor()
    {
        _isLoadingEditor = true;
        try
        {
            ClearAxisCollection(_availableAxes);
            ClearAxisCollection(_assignedAxes);
            AvailableAxisSearchText = string.Empty;
            AssignedAxisSearchText = string.Empty;
            EditingGroupName = SelectedGroup?.Snapshot.Name ?? string.Empty;

            if (SelectedGroup is null)
            {
                ValidationMessage = string.Empty;
                IsDirty = false;
                RaiseAxisStateProperties();
                return;
            }

            var assignedMembers = SelectedGroup.Snapshot.Members
                .Where(member => _axesByAddress.ContainsKey(member.Address))
                .ToDictionary(member => member.Address);
            foreach (var axis in _axesByAddress.Values
                         .OrderBy(axis => axis.Address.CardNo)
                         .ThenBy(axis => axis.Address.AxisNo))
            {
                if (assignedMembers.TryGetValue(axis.Address, out var member))
                {
                    AddAxisItem(_assignedAxes, new AxisAssignmentItemViewModel(axis, member.Role));
                }
                else
                {
                    AddAxisItem(_availableAxes, new AxisAssignmentItemViewModel(axis));
                }
            }

            RefreshRoleOptions();
            ValidationMessage = string.Empty;
            IsDirty = false;
            RaiseAxisStateProperties();
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private void SetSelectedGroup(AxisGroupItemViewModel? value)
    {
        _isSettingSelection = true;
        try
        {
            if (SetProperty(ref _selectedGroup, value, nameof(SelectedGroup)))
            {
                LoadSelectedGroupEditor();
                RaisePropertyChanged(nameof(HasSelectedGroup));
                RaisePropertyChanged(nameof(IsEditorEnabled));
                RaiseCommandStates();
            }
        }
        finally
        {
            _isSettingSelection = false;
        }
    }

    private async Task<bool> SaveCurrentGroupAsync()
    {
        var current = SelectedGroup!;
        var updated = CreateEditingModel(current.Id);
        try
        {
            IsBusy = true;
            var models = Groups.Select(group =>
                    ReferenceEquals(group, current)
                        ? updated
                        : Clone(group.Snapshot))
                .Where((_, index) => !Groups[index].IsTransient || ReferenceEquals(Groups[index], current))
                .ToArray();
            await _groupConfigService.SaveAsync(models);
            current.Accept(
                updated,
                updated.Members.Count(member => _axesByAddress.ContainsKey(member.Address)));
            current.MarkPersisted();
            _editingGroupName = updated.Name;
            RaisePropertyChanged(nameof(EditingGroupName));
            IsDirty = false;
            ValidationMessage = string.Empty;
            StatusMessage = $"分组“{updated.Name}”已保存。";
            return true;
        }
        catch (Exception exception)
        {
            StatusMessage = $"保存分组失败：{exception.Message}";
            IsDirty = true;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ResolveUnsavedChangesAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        var result = _promptService is null
            ? ConfigurationPromptResult.DiscardAndContinue
            : await _promptService.ConfirmUnsavedChangesAsync();
        return result switch
        {
            ConfigurationPromptResult.SaveAndContinue => await SaveGroupAsync(),
            ConfigurationPromptResult.DiscardAndContinue => DiscardCurrentEdits(),
            _ => false
        };
    }

    private bool DiscardCurrentEdits()
    {
        var current = SelectedGroup;
        if (current?.IsTransient == true)
        {
            Groups.Remove(current);
            SetSelectedGroup(Groups.FirstOrDefault());
        }
        else
        {
            LoadSelectedGroupEditor();
        }

        IsDirty = false;
        StatusMessage = string.Empty;
        return true;
    }

    private AxisGroupConfig CreateEditingModel(string id) => new()
    {
        Id = id,
        Name = EditingGroupName.Trim(),
        Members = _assignedAxes
            .Select(item => new AxisGroupMember
            {
                Address = item.Address,
                Role = item.Role
            })
            .Concat(SelectedGroup?.Snapshot.Members
                .Where(member => !_axesByAddress.ContainsKey(member.Address))
                .Select(CloneMember) ?? [])
            .OrderBy(member => member.Address.CardNo)
            .ThenBy(member => member.Address.AxisNo)
            .ToList()
    };

    private void RecalculateDirtyState()
    {
        if (_isLoadingEditor)
        {
            return;
        }

        if (SelectedGroup is null)
        {
            IsDirty = false;
            return;
        }

        if (SelectedGroup.IsTransient)
        {
            IsDirty = true;
            return;
        }

        var persistedMembers = SelectedGroup.Snapshot.Members
            .Where(member => _axesByAddress.ContainsKey(member.Address))
            .OrderBy(member => member.Address.CardNo)
            .ThenBy(member => member.Address.AxisNo);
        var editingMembers = _assignedAxes
            .Select(item => new AxisGroupMember
            {
                Address = item.Address,
                Role = item.Role
            })
            .OrderBy(member => member.Address.CardNo)
            .ThenBy(member => member.Address.AxisNo);
        IsDirty = !string.Equals(
                      EditingGroupName.Trim(),
                      SelectedGroup.Snapshot.Name,
                      StringComparison.Ordinal)
                  || !persistedMembers.SequenceEqual(editingMembers);
    }

    private void ValidateEditingName()
    {
        if (SelectedGroup is null)
        {
            ValidationMessage = string.Empty;
            return;
        }

        ValidationMessage = _groupConfigService.ValidateName(
            EditingGroupName,
            Groups.Select(group => Clone(group.Snapshot)),
            SelectedGroup.Id) ?? string.Empty;
        RaiseCommandStates();
    }

    private static AxisGroupConfig Clone(AxisGroupConfig group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Members = group.Members.Select(CloneMember).ToList()
    };

    private void MoveItems(
        IReadOnlyCollection<AxisAssignmentItemViewModel> items,
        bool addToGroup)
    {
        if (items.Count == 0)
        {
            return;
        }

        var source = addToGroup ? _availableAxes : _assignedAxes;
        var target = addToGroup ? _assignedAxes : _availableAxes;
        var targetAddresses = target.Select(item => item.Address).ToHashSet();
        foreach (var item in items)
        {
            if (!source.Contains(item))
            {
                continue;
            }

            RemoveAxisItem(source, item);
            if (targetAddresses.Add(item.Address))
            {
                AddAxisItem(target, item.Copy(AxisRole.None));
            }
        }

        SortAxes(source);
        SortAxes(target);
        RefreshRoleOptions();
        RecalculateDirtyState();
        RaiseAxisStateProperties();
    }

    private void AddAxisItem(
        ObservableCollection<AxisAssignmentItemViewModel> collection,
        AxisAssignmentItemViewModel item)
    {
        item.PropertyChanged += OnAxisItemPropertyChanged;
        collection.Add(item);
    }

    private void RemoveAxisItem(
        ObservableCollection<AxisAssignmentItemViewModel> collection,
        AxisAssignmentItemViewModel item)
    {
        item.PropertyChanged -= OnAxisItemPropertyChanged;
        collection.Remove(item);
    }

    private void ClearAxisCollection(ObservableCollection<AxisAssignmentItemViewModel> collection)
    {
        foreach (var item in collection)
        {
            item.PropertyChanged -= OnAxisItemPropertyChanged;
        }

        collection.Clear();
    }

    private void SortAxes(ObservableCollection<AxisAssignmentItemViewModel> collection)
    {
        var ordered = collection
            .OrderBy(item => item.Address.CardNo)
            .ThenBy(item => item.Address.AxisNo)
            .ToArray();
        ClearAxisCollection(collection);
        foreach (var item in ordered)
        {
            item.IsSelected = false;
            AddAxisItem(collection, item);
        }
    }

    private void OnAxisItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AxisAssignmentItemViewModel.IsSelected))
        {
            RaisePropertyChanged(nameof(SelectedAvailableCount));
            RaisePropertyChanged(nameof(SelectedAssignedCount));
            RaisePropertyChanged(nameof(SelectedAxisCount));
            RaiseCommandStates();
        }
        else if (args.PropertyName == nameof(AxisAssignmentItemViewModel.Role))
        {
            RefreshRoleOptions();
            AssignedAxesView.Refresh();
            RecalculateDirtyState();
        }
    }

    private bool FilterAvailableAxis(object item) =>
        MatchesSearch(item, AvailableAxisSearchText);

    private bool FilterAssignedAxis(object item) =>
        MatchesSearch(item, AssignedAxisSearchText, includeRole: true);

    private static bool MatchesSearch(
        object item,
        string searchText,
        bool includeRole = false)
    {
        if (item is not AxisAssignmentItemViewModel axis)
        {
            return false;
        }

        var query = searchText.Trim();
        return query.Length == 0
               || axis.AxisName.Contains(query, StringComparison.OrdinalIgnoreCase)
               || MatchesAxisNumber(axis, query)
               || (includeRole
                   && axis.RoleText.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesAxisNumber(
        AxisAssignmentItemViewModel axis,
        string query)
    {
        const string axisPrefix = "Axis";
        var numberText = query.StartsWith(axisPrefix, StringComparison.OrdinalIgnoreCase)
            ? query[axisPrefix.Length..].Trim()
            : query;
        if (int.TryParse(numberText, out var axisNo))
        {
            return axis.Address.AxisNo == axisNo;
        }

        var addressParts = query.Split(
            '/',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return addressParts.Length == 2
               && int.TryParse(addressParts[0], out var cardNo)
               && int.TryParse(addressParts[1], out var addressAxisNo)
               && axis.Address.CardNo == cardNo
               && axis.Address.AxisNo == addressAxisNo;
    }

    private void RefreshRoleOptions()
    {
        var missingMemberRoles = SelectedGroup?.Snapshot.Members
            .Where(member => member.Role != AxisRole.None
                             && !_axesByAddress.ContainsKey(member.Address))
            .Select(member => member.Role)
            .ToHashSet() ?? [];
        foreach (var item in _assignedAxes)
        {
            var usedByOthers = _assignedAxes
                .Where(other => !ReferenceEquals(other, item) && other.Role != AxisRole.None)
                .Select(other => other.Role)
                .ToHashSet();
            usedByOthers.UnionWith(missingMemberRoles);
            item.SetAvailableRoles(
                Enum.GetValues<AxisRole>()
                    .Where(role => role == AxisRole.None
                                   || role == item.Role
                                   || !usedByOthers.Contains(role)));
        }
    }

    private static AxisGroupMember CloneMember(AxisGroupMember member) => new()
    {
        Address = member.Address,
        Role = member.Role
    };

    private void RaiseAxisStateProperties()
    {
        AvailableAxesView.Refresh();
        AssignedAxesView.Refresh();
        RaisePropertyChanged(nameof(SelectedAvailableCount));
        RaisePropertyChanged(nameof(SelectedAssignedCount));
        RaisePropertyChanged(nameof(SelectedAxisCount));
        RaisePropertyChanged(nameof(AssignedAxisCount));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        AddSelectedAxesCommand.RaiseCanExecuteChanged();
        AddAllFilteredAxesCommand.RaiseCanExecuteChanged();
        RemoveSelectedAxesCommand.RaiseCanExecuteChanged();
        RemoveAllFilteredAxesCommand.RaiseCanExecuteChanged();
        NewGroupCommand.RaiseCanExecuteChanged();
        SaveGroupCommand.RaiseCanExecuteChanged();
        DeleteGroupCommand.RaiseCanExecuteChanged();
    }
}
