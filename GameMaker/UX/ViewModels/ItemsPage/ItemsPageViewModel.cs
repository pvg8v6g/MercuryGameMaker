using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameMaker.UX.Components.GridArea;

namespace GameMaker.UX.ViewModels.ItemsPage;

public class ItemsPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Consumable>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    private int HitboxViewGrid => 144;

    private int AnchorBoxSize => 48;

    public ObservableCollection<Area> PreviewsHitboxes { get; } = [];

    public ObservableCollection<GridLine> PreviewGridLines { get; } = [];

    public double HitboxPreviewScale
    {
        get;
        private set => SetField(ref field, value);
    } = 1.0;

    public double HitboxPreviewOffsetX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double HitboxPreviewOffsetY
    {
        get;
        private set => SetField(ref field, value);
    }

    public ObservableCollection<ConsumableAttributeViewModel> AttributeChanges { get; } = [];

    public ObservableCollection<ConsumableToggleViewModel> AddStates { get; } = [];

    public ObservableCollection<ConsumableToggleViewModel> RemoveStates { get; } = [];

    protected override ObservableCollection<Consumable> EntityCollection => gameDataService.Consumables;

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshStats();
        LinkHitboxes();
        return Task.CompletedTask;
    }

    private void OnHitboxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshHitboxes();
    }

    #endregion

    #region Private methods

    private void RefreshStats()
    {
        AttributeChanges.Clear();
        AddStates.Clear();
        RemoveStates.Clear();

        if (SelectedEntity is null) return;

        AttributeChanges.Add(new ConsumableAttributeViewModel("Life", 478, gameDataService.LifeAttributeGuid, SelectedEntity));
        AttributeChanges.Add(new ConsumableAttributeViewModel("Mana", 523, gameDataService.ManaAttributeGuid, SelectedEntity));

        foreach (var attribute in gameDataService.Attributes)
        {
            AttributeChanges.Add(new ConsumableAttributeViewModel(attribute.Name, attribute.Icon, attribute.Guid, SelectedEntity));
        }

        foreach (var state in gameDataService.States)
        {
            AddStates.Add(new ConsumableToggleViewModel(state.Name, state.Icon, SelectedEntity.AddStates, state.Guid));
            RemoveStates.Add(new ConsumableToggleViewModel(state.Name, state.Icon, SelectedEntity.RemoveStates, state.Guid));
        }

        OnPropertyChanged(nameof(AttributeChanges));
        OnPropertyChanged(nameof(AddStates));
        OnPropertyChanged(nameof(RemoveStates));
    }

    private void LinkHitboxes()
    {
        if (_trackedHitboxes is not null) _trackedHitboxes.CollectionChanged -= OnHitboxesCollectionChanged;
        _trackedHitboxes = SelectedEntity?.Hitboxes;
        if (_trackedHitboxes is not null) _trackedHitboxes.CollectionChanged += OnHitboxesCollectionChanged;
        RefreshHitboxes();
    }

    private void RefreshHitboxes()
    {
        PreviewsHitboxes.Clear();
        HitboxPreviewScale = 1.0;
        HitboxPreviewOffsetX = 0.0;
        HitboxPreviewOffsetY = 0.0;

        if (SelectedEntity is null)
        {
            RefreshGridLines();
            return;
        }

        foreach (var area in SelectedEntity.Hitboxes)
        {
            PreviewsHitboxes.Add(area);
        }

        var characterSprite = new Area { OffsetX = 0, OffsetY = 0, Width = AnchorBoxSize, Height = AnchorBoxSize };

        // include the full anchor box (0,0)-(AnchorBoxSize,AnchorBoxSize) so the caster marker always stays visible
        Area[] previews = [.. PreviewsHitboxes, characterSprite];
        var left = previews.Select(x => x.OffsetX).Min();
        var right = previews.Select(x => x.OffsetX + x.Width).Max();
        var top = previews.Select(x => x.OffsetY).Min();
        var bottom = previews.Select(x => x.OffsetY + x.Height).Max();

        var fullWidth = right - left;
        var fullHeight = bottom - top;
        var max = Math.Max(fullWidth, fullHeight);

        // with no hitboxes, keep the anchor centered instead of pinned to the top-left
        if (max <= 0)
        {
            HitboxPreviewScale = 1.0;
            HitboxPreviewOffsetX = (HitboxViewGrid - AnchorBoxSize) / 2.0;
            HitboxPreviewOffsetY = (HitboxViewGrid - AnchorBoxSize) / 2.0;
            RefreshGridLines();
            return;
        }

        var scale = HitboxViewGrid / (double) max;
        if (scale > 1.0)
        {
            HitboxPreviewOffsetX = (HitboxViewGrid - fullWidth) / 2.0 - left;
            HitboxPreviewOffsetY = (HitboxViewGrid - fullHeight) / 2.0 - top;
            HitboxPreviewScale = 1.0;
        }
        else
        {
            HitboxPreviewOffsetX = (HitboxViewGrid - fullWidth * scale) / 2.0 - left * scale;
            HitboxPreviewOffsetY = (HitboxViewGrid - fullHeight * scale) / 2.0 - top * scale;
            HitboxPreviewScale = scale;
        }

        RefreshGridLines();
    }

    private void RefreshGridLines()
    {
        PreviewGridLines.Clear();

        var scale = HitboxPreviewScale;
        if (scale <= 0) return;

        // grid lines are computed directly in preview (screen) coordinates so they always cover the whole preview
        var step = AnchorBoxSize * scale;
        if (step <= 0) return;

        var size = (double) HitboxViewGrid;

        // phase so a grid line falls exactly on the caster origin (content 0,0 -> screen offset)
        var startX = HitboxPreviewOffsetX % step;
        if (startX > 0) startX -= step;
        for (var x = startX; x <= size; x += step)
        {
            PreviewGridLines.Add(new GridLine(x, 0, x, size));
        }

        var startY = HitboxPreviewOffsetY % step;
        if (startY > 0) startY -= step;
        for (var y = startY; y <= size; y += step)
        {
            PreviewGridLines.Add(new GridLine(0, y, size, y));
        }
    }

    #endregion

    #region Fields

    private ObservableCollection<Area>? _trackedHitboxes;

    #endregion
}
