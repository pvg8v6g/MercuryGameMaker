using GameLibrary.Commands;
using GameLibrary.Models.Items;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Components.DropListView;

public class DropEditorModel : PropertyChangedUpdater
{
    public Item[] AllItems
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(CanSave));
        }
    } = [];

    public Guid? SelectedItemGuid
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public string PercentText
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(CanSave));
        }
    } = string.Empty;

    public bool CanSave => SelectedItemGuid.HasValue
                           && SelectedItemGuid.Value != Guid.Empty
                           && !PercentText.IsNullOrEmpty()
                           && int.TryParse(PercentText, out _);

    public Action? OnSave { get; set; }

    public RelayCommand SaveCommand => new(() =>
    {
        if (CanSave) OnSave?.Invoke();
    });
}
