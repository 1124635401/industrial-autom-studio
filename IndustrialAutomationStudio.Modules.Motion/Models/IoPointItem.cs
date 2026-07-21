using Prism.Mvvm;

namespace IndustrialAutomationStudio.Modules.Motion.Models;

public enum IoPointType
{
    DI,
    DO
}

public sealed class IoPointItem : BindableBase
{
    private string _displayName;
    private string _editName;
    private string _nameBeforeEdit;
    private bool? _isOn;
    private bool _canOperate;
    private bool _isEditing;
    private bool _isWriting;

    public IoPointItem(int index, IoPointType type, string? displayName = null)
    {
        if (index < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Type = type;
        Address = $"{type}_{index}";
        _displayName = NormalizeName(displayName);
        _editName = _displayName;
        _nameBeforeEdit = _displayName;
    }

    public int Index { get; }
    public string Address { get; }
    public IoPointType Type { get; }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, NormalizeName(value));
    }

    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value ?? string.Empty);
    }

    public bool? IsOn
    {
        get => _isOn;
        set => SetProperty(ref _isOn, value);
    }

    public bool CanOperate
    {
        get => _canOperate;
        set => SetProperty(ref _canOperate, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set => SetProperty(ref _isEditing, value);
    }

    public bool IsWriting
    {
        get => _isWriting;
        set => SetProperty(ref _isWriting, value);
    }

    public void BeginEdit()
    {
        _nameBeforeEdit = DisplayName;
        EditName = DisplayName;
        IsEditing = true;
    }

    public void CommitEdit()
    {
        DisplayName = EditName;
        EditName = DisplayName;
        IsEditing = false;
    }

    public void CancelEdit()
    {
        DisplayName = _nameBeforeEdit;
        EditName = DisplayName;
        IsEditing = false;
    }

    public void RestoreDisplayName(string displayName)
    {
        DisplayName = displayName;
        EditName = DisplayName;
        IsEditing = false;
    }

    private string NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Address : value.Trim();
}
