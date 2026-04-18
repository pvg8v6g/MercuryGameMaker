using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.UX.Models.ActorsPage;
using MercuryLibrary.Extensions;

namespace GameMaker.UX.ViewModels.ActorsPage;

public partial class ActorsPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService) : BaseViewModel<Fighter>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public string CharacterFolderPath => graphicsService.GetCharacterPath();

    public string FaceFolderPath => Path.Combine(locationService.GraphicsDirectory!, "Faces");

    public int HitboxViewGrid => 144;

    public ObservableCollection<ActorElementModel> ElementResistanceStats { get; } = [];

    public ObservableCollection<EquipmentSlotModel> FighterEquipmentSlots { get; } = [];

    protected override ObservableCollection<Fighter> EntityCollection => gameDataService.Actors;

    public CroppedImage? AnchorImage
    {
        get;
        private set => SetField(ref field, value);
    }

    /*
     * selected direction of the hitbox viewer - independent of the character direction
     */
    public Direction SelectedDirection
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = RefreshAnchor();
        }
    } = Direction.None;

    public ObservableCollection<Area> SelectedDirectionHitboxes
    {
        get;
        set
        {
            field.CollectionChanged -= OnHitboxesCollectionChanged;
            SetField(ref field, value);
            field.CollectionChanged += OnHitboxesCollectionChanged;
            RefreshHitboxes();
        }
    } = [];

    public ObservableCollection<Area> PreviewsHitboxes { get; } = [];

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

    public string? CharacterName
    {
        get;
        set => SetField(ref field, value);
    }

    /*
     * true index - disregarding direction
     */
    public int CharacterIndex
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Commands

    public RelayCommand<SpriteSelectedArgs> SelectSpriteCommand => new(SelectSpriteAction);

    #endregion

    #region Fields

    private Rect? _spriteBox;

    private double SpriteWidth => _spriteBox?.Width * HitboxPreviewScale ?? 0.0;

    private double SpriteHeight => _spriteBox?.Height * HitboxPreviewScale ?? 0.0;

    #endregion

    #region Actions

    private void SelectSpriteAction(SpriteSelectedArgs args)
    {
        if (SelectedEntity is null) return;
        SelectedEntity.CharacterName = args.FileName;
        SelectedEntity.CharacterIndex = args.Index;
        SelectedEntity.CharacterDirection = args.Direction;
        _ = RefreshAnchor();
    }

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        RefreshStats();
        RefreshEquipment();
        SelectedDirection = Direction.None;
        await RefreshAnchor();

        CharacterName = SelectedEntity?.CharacterName;
        if (SelectedEntity is null || CharacterName.IsNullOrEmpty())
        {
            SelectedDirectionHitboxes = [];
            CharacterIndex = 1;
            return;
        }

        _spriteBox = (await graphicsService.GetCharacter(SelectedEntity.CharacterName!, SelectedEntity.CharacterIndex, SelectedDirection)).Rect;
        LinkHitboxes(SelectedDirection);
        CharacterIndex = graphicsService.GetCharacterIndexFromDirection(
            SelectedEntity.CharacterName!,
            SelectedEntity.CharacterIndex,
            SelectedEntity.CharacterDirection);
    }

    private void OnHitboxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshHitboxes();
    }

    #endregion

    #region Private Methods

    private async Task RefreshAnchor()
    {
        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty())
        {
            AnchorImage = null;
            return;
        }

        var direction = SelectedDirection is Direction.None ? Direction.Down : SelectedDirection;
        AnchorImage = await graphicsService.GetCharacter(SelectedEntity!.CharacterName!, SelectedEntity.CharacterIndex, direction);
    }

    private void RefreshStats()
    {
        ElementResistanceStats.Clear();

        if (SelectedEntity is null) return;
        foreach (var element in GameDataService.Elements)
        {
            ElementResistanceStats.Add(new ActorElementModel(element, SelectedEntity));
        }
    }

    private void RefreshEquipment()
    {
        FighterEquipmentSlots.Clear();

        if (SelectedEntity is null) return;

        foreach (var kvp in SelectedEntity.Equipment)
        {
            var filtered = GameDataService.Equipment
                .Where(e => e.EquipmentLocation == kvp.Value.Location)
                .ToArray();
            FighterEquipmentSlots.Add(new EquipmentSlotModel(kvp.Key, kvp.Value, filtered));
        }
    }

    private void LinkHitboxes(Direction direction)
    {
        if (SelectedEntity is null)
        {
            SelectedDirectionHitboxes = [];
        }
        else
        {
            SelectedEntity.Hitboxes.TryAdd(direction, []);
            SelectedDirectionHitboxes = SelectedEntity.Hitboxes[direction];
        }
    }

    private void RefreshHitboxes()
    {
        PreviewsHitboxes.Clear();
        CalculateSpriteDefaultOffset();

        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty()) return;
        Area[] hitboxes = [];
        if (SelectedDirection is not Direction.None) hitboxes = [..SelectedEntity.Hitboxes.GetValueOrDefault(SelectedDirection, [])];
        hitboxes = [..hitboxes, ..SelectedEntity.Hitboxes.GetValueOrDefault(Direction.None, [])];
        if (hitboxes.Length == 0)
        {
            HitboxPreviewScale = 1.0;
            CalculateSpriteDefaultOffset();
            return;
        }

        foreach (var area in hitboxes)
        {
            PreviewsHitboxes.Add(area);
        }

        if (_spriteBox is null)
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            HitboxPreviewScale = 1.0;
            return;
        }

        var characterSprite = new Area { OffsetX = 0, OffsetY = 0, Width = (int) SpriteWidth, Height = (int) SpriteHeight };
        Area[] previews = [..PreviewsHitboxes, characterSprite];
        var left = previews.Select(x => x.OffsetX).Min();
        var right = previews.Select(x => x.OffsetX + x.Width).Max();
        var top = previews.Select(x => x.OffsetY).Min();
        var bottom = previews.Select(x => x.OffsetY + x.Height).Max();

        var fullWidth = right - left;
        var fullHeight = bottom - top;
        var max = Math.Max(fullWidth, fullHeight);
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
    }

    // reset offset to center
    private void CalculateSpriteDefaultOffset()
    {
        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty() || _spriteBox is null)
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            return;
        }

        HitboxPreviewOffsetX = (HitboxViewGrid - SpriteWidth) / 2.0;
        HitboxPreviewOffsetY = (HitboxViewGrid - SpriteHeight) / 2.0;
    }

    #endregion
}
