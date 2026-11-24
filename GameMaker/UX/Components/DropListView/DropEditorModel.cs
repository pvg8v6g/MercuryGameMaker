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
            UpdateFilteredItems();
            OnPropertyChanged(nameof(PickedItem));
            OnPropertyChanged(nameof(CanSave));
        }
    } = [];

    // what the search box has typed: the item list shows only items whose name contains it
    public string SearchText
    {
        get;
        set
        {
            SetField(ref field, value);
            UpdateFilteredItems();
        }
    } = string.Empty;

    public Item[] FilteredItems
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public Guid? SelectedItemGuid
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(PickedItem));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    // bound to the item list. Searching swaps the list's items, and the list then reports no selection: that null is
    // ignored, so typing in the search box never loses the item already picked.
    public Item? PickedItem
    {
        get => AllItems.FirstOrDefault(x => x.Guid == SelectedItemGuid);
        set
        {
            if (value is null) return;
            SelectedItemGuid = value.Guid;
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

    private void UpdateFilteredItems()
    {
        FilteredItems = SearchText.IsNullOrEmpty()
            ? AllItems
            : AllItems.Where(x => (x.Name ?? string.Empty).Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
