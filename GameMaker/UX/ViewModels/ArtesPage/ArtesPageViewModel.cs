using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GameLibrary.Commands;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Artes;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.ViewModels.Popups.EntityEditor;

namespace GameMaker.UX.ViewModels.ArtesPage;

public class ArtesPageViewModel(IGameDataService gameDataService, IJsonService jsonService, INavigationService navigationService)
    : BaseViewModel<Arte>(jsonService)
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

    public ObservableCollection<ArteToggleViewModel> AddStates { get; } = [];

    public ObservableCollection<ArteToggleViewModel> RemoveStates { get; } = [];

    public ObservableCollection<ArteToggleViewModel> Elements { get; } = [];

    protected override ObservableCollection<Arte> EntityCollection => gameDataService.Artes;

    public int SelectedProjectileIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public Entity? CopiedProjectile
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Projectile relays

    public RelayCommand<string?> NewProjectileCommand => new(NewProjectile);

    public RelayCommand CopyProjectileCommand => new(CopyProjectile);

    public RelayCommand PasteProjectileCommand => new(PasteProjectile);

    public RelayCommand DeleteProjectileCommand => new(DeleteProjectile);

    public AsyncRelayCommand EditProjectileCommand => new(EditProjectile);

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshToggles();
        LinkHitboxes();
        SelectedProjectileIndex = -1;
        return Task.CompletedTask;
    }

    private void NewProjectile(string? name = null)
    {
        if (SelectedEntity is null) return;

        var projectiles = SelectedEntity.Projectiles;
        var newObject = new Entity
        {
            Id = Calculations.GetNextId(projectiles.Select(x => x.Id).ToArray()),
            Name = name ?? "New Projectile"
        };

        var index = projectiles.AddSorted(newObject, x => x.Id);
        SelectedProjectileIndex = index;
    }

    private void CopyProjectile()
    {
        if (SelectedEntity is null) return;
        if (SelectedProjectileIndex < 0 || SelectedProjectileIndex >= SelectedEntity.Projectiles.Count) return;
        CopiedProjectile = SelectedEntity.Projectiles[SelectedProjectileIndex];
    }

    private void PasteProjectile()
    {
        if (SelectedEntity is null || CopiedProjectile is null) return;

        var clone = JsonService.Clone(CopiedProjectile);
        if (clone is null) return;

        clone.Guid = Guid.NewGuid();
        clone.Id = Calculations.GetNextId(SelectedEntity.Projectiles.Select(x => x.Id).ToArray());

        var index = SelectedEntity.Projectiles.AddSorted(clone, x => x.Id);
        SelectedProjectileIndex = index;
    }

    private void DeleteProjectile()
    {
        if (SelectedEntity is null) return;

        var index = SelectedProjectileIndex;
        if (index < 0 || index >= SelectedEntity.Projectiles.Count) return;

        SelectedEntity.Projectiles.RemoveAt(index);

        if (SelectedEntity.Projectiles.Count == 0)
        {
            SelectedProjectileIndex = -1;
        }
        else if (index >= SelectedEntity.Projectiles.Count)
        {
            SelectedProjectileIndex = SelectedEntity.Projectiles.Count - 1;
        }
        else
        {
            SelectedProjectileIndex = index;
        }
    }

    private async Task EditProjectile()
    {
        if (SelectedEntity is null) return;
        if (SelectedProjectileIndex < 0 || SelectedProjectileIndex >= SelectedEntity.Projectiles.Count) return;

        var projectile = SelectedEntity.Projectiles[SelectedProjectileIndex];
        var popupViewModel = new EntityEditorViewModel(projectile, gameDataService, JsonService, navigationService);
        await navigationService.ShowEntityEditorPopup(popupViewModel);
    }

    private void OnHitboxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshHitboxes();
    }

    #endregion

    #region Private methods

    private void RefreshToggles()
    {
        AddStates.Clear();
        RemoveStates.Clear();
        Elements.Clear();

        if (SelectedEntity is null) return;

        foreach (var state in gameDataService.States)
        {
            AddStates.Add(new ArteToggleViewModel(state.Name, state.Icon, SelectedEntity.AddStates, state.Guid));
            RemoveStates.Add(new ArteToggleViewModel(state.Name, state.Icon, SelectedEntity.RemoveStates, state.Guid));
        }

        foreach (var element in gameDataService.Elements)
        {
            Elements.Add(new ArteToggleViewModel(element.Name, element.Icon, SelectedEntity.Elements, element.Guid));
        }

        OnPropertyChanged(nameof(AddStates));
        OnPropertyChanged(nameof(RemoveStates));
        OnPropertyChanged(nameof(Elements));
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
