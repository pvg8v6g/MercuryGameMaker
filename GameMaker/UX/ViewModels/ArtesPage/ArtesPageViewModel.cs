using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GameLibrary.Enumerations;
using GameLibrary.Commands;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.ViewModels.Popups.EntityEditor;

namespace GameMaker.UX.ViewModels.ArtesPage;

public class ArtesPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    INavigationService navigationService)
    : BaseViewModel<Arte>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    // the preview fighter, the hitboxes and the caster animation for the picked direction, as the game shows them
    public ArteAnimationPreviewViewModel AnimationPreview { get; } = new(gameDataService, graphicsService);

    private int HitboxViewGrid => 144;

    private int AnchorBoxSize => 48;

    public ObservableCollection<Area> PreviewsHitboxes { get; } = [];

    // the ways an arte that turns with its user can have its own hitboxes drawn for
    public Direction[] HitboxDirections { get; } = [Direction.Down, Direction.Left, Direction.Right, Direction.Up];

    public Direction HitboxDirection
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HitboxDirectionIndex));
            LinkHitboxes();
            OnPropertyChanged(nameof(SelectedAnimationPlacement));
            _ = RefreshPreviewImage();
        }
    } = Direction.Down;

    // HitboxDirection by its place in HitboxDirections, for the Animations panel's direction radio buttons. Switching
    // buttons passes through "nothing picked" (-1, or a null SelectedItem, which crashes a binding to an enum), and
    // that's ignored so the direction stays until the new one is picked
    public int HitboxDirectionIndex
    {
        get => Array.IndexOf(HitboxDirections, HitboxDirection);
        set
        {
            if (value >= 0 && value < HitboxDirections.Length) HitboxDirection = HitboxDirections[value];
        }
    }

    // where the one animation goes for the picked direction, when the arte moves and rotates one animation
    public ArteAnimationPlacement? SelectedAnimationPlacement => SelectedEntity?.AnimationFor(ShownDirection);

    public bool ShowsAnimationPlacement => SelectedEntity is { DirectionRotate: true, DirectionAnimations: DirectionAnimations.MoveAndRotate };

    // a line under the turning mode saying what it does, so the choice reads without a tooltip
    public string DirectionAnimationsHint => SelectedEntity?.DirectionAnimations == DirectionAnimations.MoveAndRotate
        ? "One animation, moved and turned for each way the user faces. Set it for each direction under Placement."
        : "A separate animation for each way the user faces: the picked one faces down, the next one by Id faces left, then right, then up.";

    // the direction picker only shows for an arte that turns with its user, the others only have the down hitboxes
    public bool ShowsHitboxDirections => SelectedEntity?.DirectionRotate == true;

    private Direction ShownDirection => ShowsHitboxDirections ? HitboxDirection : Direction.Down;

    // the hitboxes being drawn: the ones for the picked direction
    public ObservableCollection<Area>? SelectedDirectionHitboxes
    {
        get;
        private set => SetField(ref field, value);
    }

    // a fighter shown standing on the anchor in the hitbox picker, facing the picked direction, with their own
    // hitbox outlined, so the arte's hitboxes can be drawn over a real body. Only for the editor, never saved
    public Fighter[] PreviewFighters => [.. gameDataService.Actors, .. gameDataService.Enemies];

    public Fighter? PreviewFighter
    {
        get;
        set
        {
            SetField(ref field, value);
            PreviewFighterHitboxes = value?.Hitboxes.GetValueOrDefault(Direction.None);
            _ = RefreshPreviewImage();
        }
    }

    public CroppedImage? PreviewImage
    {
        get;
        private set => SetField(ref field, value);
    }

    public ObservableCollection<Area>? PreviewFighterHitboxes
    {
        get;
        private set => SetField(ref field, value);
    }

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


    #region Hitbox relays

    // gives every other direction that has no hitboxes yet the down hitboxes turned to face it (what the game would do
    // anyway), as a starting point to adjust. Directions that already have hitboxes are left alone
    public RelayCommand FillFromDownCommand => new(() =>
    {
        if (SelectedEntity is not { } arte) return;

        foreach (var direction in HitboxDirections.Where(x => x != Direction.Down))
        {
            var hitboxes = arte.HitboxesFor(direction);
            if (hitboxes.Count > 0) continue;

            foreach (var box in arte.Hitboxes) hitboxes.Add(Arte.TurnedFromDown(box, direction, AnchorBoxSize));
        }

        RefreshHitboxes();
    });

    #endregion

    #region Projectile relays

    public AsyncRelayCommand<string?> NewProjectileCommand => new(NewProjectile);

    public RelayCommand CopyProjectileCommand => new(CopyProjectile);

    public RelayCommand PasteProjectileCommand => new(PasteProjectile);

    public RelayCommand DeleteProjectileCommand => new(DeleteProjectile);

    public AsyncRelayCommand EditProjectileCommand => new(EditProjectile);

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshToggles();
        WatchArte();
        LinkHitboxes();
        SelectedProjectileIndex = -1;
        PreviewFighter ??= PreviewFighters.FirstOrDefault();
        _ = RefreshPreviewImage();
        return Task.CompletedTask;
    }

    // turning Rotate Arte With Character Direction on or off shows or hides the direction picker
    private void OnArtePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Arte.DirectionAnimations))
        {
            OnPropertyChanged(nameof(ShowsAnimationPlacement));
            OnPropertyChanged(nameof(DirectionAnimationsHint));
        }
        if (e.PropertyName is nameof(Arte.DirectionAnimations) or nameof(Arte.CasterAnimation)) RefreshAnimationPreview();
        if (e.PropertyName != nameof(Arte.DirectionRotate)) return;

        OnPropertyChanged(nameof(ShowsHitboxDirections));
        OnPropertyChanged(nameof(ShowsAnimationPlacement));
        OnPropertyChanged(nameof(SelectedAnimationPlacement));
        LinkHitboxes();
        _ = RefreshPreviewImage();
    }

    private async Task NewProjectile(string? name = null)
    {
        if (SelectedEntity is null) return;

        var projectiles = SelectedEntity.Projectiles;
        var newObject = new Entity
        {
            Id = Calculations.GetNextId([.. projectiles.Select(x => x.Id)]),
            Name = name ?? "New Projectile"
        };

        var index = projectiles.AddSorted(newObject, x => x.Id);
        SelectedProjectileIndex = index;

        await EditProjectile();
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
        clone.Id = Calculations.GetNextId([.. SelectedEntity.Projectiles.Select(x => x.Id)]);

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
        var popupViewModel = new EntityEditorViewModel(projectile, gameDataService, JsonService, graphicsService, locationService, navigationService, "Projectile");
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

    private void WatchArte()
    {
        if (_watchedArte is not null)
        {
            _watchedArte.PropertyChanged -= OnArtePropertyChanged;
            foreach (var placement in Placements(_watchedArte)) placement.PropertyChanged -= OnPlacementChanged;
        }

        _watchedArte = SelectedEntity;
        if (_watchedArte is not null)
        {
            _watchedArte.PropertyChanged += OnArtePropertyChanged;
            foreach (var placement in Placements(_watchedArte)) placement.PropertyChanged += OnPlacementChanged;
        }

        OnPropertyChanged(nameof(ShowsHitboxDirections));
        OnPropertyChanged(nameof(ShowsAnimationPlacement));
        OnPropertyChanged(nameof(SelectedAnimationPlacement));
        OnPropertyChanged(nameof(DirectionAnimationsHint));
    }

    private void LinkHitboxes()
    {
        if (_trackedHitboxes is not null) _trackedHitboxes.CollectionChanged -= OnHitboxesCollectionChanged;
        _trackedHitboxes = SelectedEntity?.HitboxesFor(ShownDirection);
        if (_trackedHitboxes is not null) _trackedHitboxes.CollectionChanged += OnHitboxesCollectionChanged;
        SelectedDirectionHitboxes = _trackedHitboxes;
        RefreshHitboxes();
    }

    private IEnumerable<ArteAnimationPlacement> Placements(Arte arte) => HitboxDirections.Select(arte.AnimationFor);

    // moving, turning or mirroring the animation shows straight away in the preview
    private void OnPlacementChanged(object? sender, PropertyChangedEventArgs e) => RefreshAnimationPreview();

    private void RefreshAnimationPreview()
    {
        _ = AnimationPreview.Rebuild(SelectedEntity, ShownDirection, PreviewImage, PreviewFighterHitboxes);
    }

    // the preview fighter's picture, facing the way being drawn
    private async Task RefreshPreviewImage()
    {
        if (PreviewFighter is not { CharacterName: { Length: > 0 } name } fighter)
        {
            PreviewImage = null;
            RefreshHitboxes();
            return;
        }

        var image = await graphicsService.GetCharacter(name, fighter.CharacterIndex, ShownDirection);
        PreviewImage = image.ImageSource is null ? null : image;
        RefreshHitboxes();
    }

    private void RefreshHitboxes()
    {
        RefreshAnimationPreview();
        PreviewsHitboxes.Clear();
        HitboxPreviewScale = 1.0;
        HitboxPreviewOffsetX = 0.0;
        HitboxPreviewOffsetY = 0.0;

        if (SelectedEntity is null)
        {
            RefreshGridLines();
            return;
        }

        foreach (var area in SelectedEntity.HitboxesFor(ShownDirection))
        {
            PreviewsHitboxes.Add(area);
        }

        var characterSprite = new Area { OffsetX = 0, OffsetY = 0, Width = AnchorBoxSize, Height = AnchorBoxSize };

        // the preview fighter stands on the anchor tile: feet on it, the picture rising above it, centred
        Area[] standing = PreviewImage?.Rect is { } rect
            ? [new Area { OffsetX = (int) Math.Round((AnchorBoxSize - rect.Width) / 2.0), OffsetY = (int) Math.Round(AnchorBoxSize - rect.Height), Width = (int) rect.Width, Height = (int) rect.Height }]
            : [];

        // include the full anchor box (0,0)-(AnchorBoxSize,AnchorBoxSize) so the caster marker always stays visible
        Area[] previews = [.. PreviewsHitboxes, characterSprite, .. standing];
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
    private Arte? _watchedArte;

    #endregion
}
