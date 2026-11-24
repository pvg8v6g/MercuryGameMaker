using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.DropListView;

public sealed partial class DropListView
{
    #region Registered Properties

    public static readonly DependencyProperty DropsSourceProperty = DependencyProperty.Register(
        nameof(DropsSource), typeof(ObservableCollection<DropModel>), typeof(DropListView),
        new PropertyMetadata(null, OnDropsSourceChanged));

    public ObservableCollection<DropModel>? DropsSource
    {
        get => (ObservableCollection<DropModel>?) GetValue(DropsSourceProperty);
        set => SetValue(DropsSourceProperty, value);
    }

    private static void OnDropsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DropListView view) return;

        if (e.OldValue is ObservableCollection<DropModel> oldCollection)
        {
            oldCollection.CollectionChanged -= view.OnDropsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<DropModel> newCollection)
        {
            newCollection.CollectionChanged += view.OnDropsCollectionChanged;
        }

        view.RebuildRows();
    }

    public static readonly DependencyProperty AllItemsProperty = DependencyProperty.Register(
        nameof(AllItems), typeof(Item[]), typeof(DropListView), new PropertyMetadata(null, OnAllItemsChanged));

    public Item[]? AllItems
    {
        get => (Item[]?) GetValue(AllItemsProperty);
        set => SetValue(AllItemsProperty, value);
    }

    private static void OnAllItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DropListView view) return;
        view.Editor.AllItems = view.AllItems ?? [];
        view.RebuildRows();
    }

    #endregion

    #region Properties

    public ObservableCollection<DropRowModel> Rows { get; } = [];

    public DropEditorModel Editor { get; } = new();

    public RelayCommand NewCommand => new(OpenNew);

    public RelayCommand EditCommand => new(OpenEdit);

    public RelayCommand DeleteCommand => new(DeleteSelected);

    #endregion

    public DropListView()
    {
        InitializeComponent();

        EditorRoot.DataContext = Editor;

        // See EngineListView: attached properties must be set from code so their
        // callbacks fire reliably for elements declared inside a UserControl's XAML.
        ListViewBehaviors.SetSelectOnRightClick(ListView, true);
        ListViewBehaviors.SetManageContextFlyoutSelection(ListView, true);
    }

    #region Actions

    private void OpenNew()
    {
        if (DropsSource is null) return;

        Editor.AllItems = AllItems ?? [];
        Editor.SearchText = string.Empty;
        Editor.SelectedItemGuid = null;
        Editor.PercentText = string.Empty;
        Editor.OnSave = () =>
        {
            if (DropsSource is null || !Editor.SelectedItemGuid.HasValue) return;

            var drop = new DropModel
            {
                DropId = Editor.SelectedItemGuid.Value,
                DropPercent = int.TryParse(Editor.PercentText, out var percent) ? percent : 0
            };

            // close the flyout first and add the row after this layout pass: changing the list while the flyout is
            // closing over it could land mid-layout and crash ("Child collection must not be modified during measure")
            EditorFlyout.Hide();
            var source = DropsSource;
            DispatcherQueue.TryEnqueue(() => source.Add(drop));
        };

        EditorFlyout.ShowAt(ListView);
    }

    private void OpenEdit()
    {
        if (ListView.SelectedItem is not DropRowModel row) return;

        var drop = row.Drop;
        Editor.AllItems = AllItems ?? [];
        Editor.SearchText = string.Empty;
        Editor.SelectedItemGuid = drop.DropId == Guid.Empty ? null : drop.DropId;
        Editor.PercentText = drop.DropPercent.ToString();
        Editor.OnSave = () =>
        {
            if (!Editor.SelectedItemGuid.HasValue) return;

            drop.DropId = Editor.SelectedItemGuid.Value;
            drop.DropPercent = int.TryParse(Editor.PercentText, out var percent) ? percent : 0;

            EditorFlyout.Hide();
            DispatcherQueue.TryEnqueue(() => ReplaceRow(drop));
        };

        EditorFlyout.ShowAt(ListView);
    }

    private void DeleteSelected()
    {
        if (DropsSource is null || ListView.SelectedItem is not DropRowModel row) return;
        DropsSource.Remove(row.Drop);
    }

    // only the rows that changed, so the list never rebuilds every row while it's on screen
    private void OnDropsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                var at = e.NewStartingIndex < 0 ? Rows.Count : e.NewStartingIndex;
                foreach (DropModel drop in e.NewItems) Rows.Insert(Math.Min(at++, Rows.Count), Row(drop));
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                foreach (DropModel drop in e.OldItems)
                {
                    var row = Rows.FirstOrDefault(x => x.Drop == drop);
                    if (row is not null) Rows.Remove(row);
                }

                break;
            default:
                RebuildRows();
                break;
        }
    }

    // the row for a drop that was edited, made again so its icon, name and chance update
    private void ReplaceRow(DropModel drop)
    {
        var index = Rows.ToList().FindIndex(x => x.Drop == drop);
        if (index >= 0) Rows[index] = Row(drop);
    }

    private DropRowModel Row(DropModel drop)
    {
        return new DropRowModel(drop, (AllItems ?? []).FirstOrDefault(x => x.Guid == drop.DropId));
    }

    private void RebuildRows()
    {
        Rows.Clear();

        if (DropsSource is null) return;

        foreach (var drop in DropsSource) Rows.Add(Row(drop));
    }

    #endregion
}
