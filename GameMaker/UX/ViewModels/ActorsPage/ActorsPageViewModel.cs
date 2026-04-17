using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
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

    public int HitboxViewGrid
    {
        get;
        set => SetField(ref field, value);
    } = 148;

    public ObservableCollection<ActorElementModel> ElementResistanceStats { get; } = [];

    public ObservableCollection<EquipmentSlotModel> FighterEquipmentSlots { get; } = [];

    protected override ObservableCollection<Fighter> EntityCollection => gameDataService.Actors;

    public Direction SelectedDirection
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = RefreshHitboxes();
        }
    } = Direction.Down;

    public ObservableCollection<Area> SelectedDirectionHitboxes
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = RefreshHitboxes();
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

    public int CharacterIndex
    {
        get;
        set
        {
            if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty())
            {
                field = 1;
                OnPropertyChanged();
                return;
            }

            _ = UpdateCharacterProperties(SelectedEntity, value);
            SetField(ref field, value);
        }
    }

    private async Task UpdateCharacterProperties(Fighter fighter, int value)
    {
        if (fighter.CharacterName.IsNullOrEmpty())
        {
            fighter.CharacterIndex = 1;
            return;
        }

        var characterPath = Path.Combine(graphicsService.GetCharacterPath(), fighter.CharacterName!);
        var segmentation = await graphicsService.GetSegmentation(characterPath);
        var divisions = segmentation.width;
        var direction = Enum.GetValues<Direction>().FirstOrDefault(x => ((int) (value / divisions)) == (int) x);
        var index = (int) (value % divisions);
        fighter.CharacterIndex = index;
        fighter.CharacterDirection = direction;
    }

    #endregion

    #region Actions

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        RefreshStats();
        RefreshEquipment();

        var characterName = SelectedEntity?.CharacterName;
        if (SelectedEntity is null || characterName.IsNullOrEmpty())
        {
            SelectedDirectionHitboxes = [];
            SelectedDirection = Direction.Down;
            CharacterIndex = 1;
            return;
        }

        SelectedEntity.Hitboxes.TryGetValue(SelectedDirection, out var hitboxValues);
        SelectedDirectionHitboxes = hitboxValues ?? [];
        var characterPath = Path.Combine(graphicsService.GetCharacterPath(), characterName);
        var divisions = (await graphicsService.GetSegmentation(characterPath)).width;
        CharacterIndex = (int) SelectedEntity.CharacterDirection * (int) divisions + SelectedEntity.CharacterIndex;
    }

    #endregion

    #region Private Methods

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

    private async Task RefreshHitboxes()
    {
        PreviewsHitboxes.Clear();
        await CalculateSpriteDefaultOffset();

        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty()) return;
        if (!SelectedEntity.Hitboxes.TryGetValue(SelectedDirection, out var value)) return;
        foreach (var area in value)
        {
            PreviewsHitboxes.Add(area);
        }


        // if (PreviewsHitboxes.Count > 0)
        // {
        //     var maxX = PreviewsHitboxes.Max(a => a.X + a.Width);
        //     var maxY = PreviewsHitboxes.Max(a => a.Y + a.Height);
        //     var maxWH = Math.Max(maxX, maxY);
        //     HitboxPreviewScale = maxWH > 0 ? Math.Min(1.0, 148.0 / maxWH) : 1.0;
        // }
        // else
        // {
        //     HitboxPreviewScale = 1.0;
        // }
        //
        // HitboxPreviewOffset = 74.0 * (1.0 - HitboxPreviewScale);
    }

    // reset offset to center
    private async Task CalculateSpriteDefaultOffset()
    {
        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty())
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            return;
        }

        var spriteBox = (await graphicsService.GetCharacter(SelectedEntity.CharacterName, SelectedEntity.CharacterIndex)).Rect;
        if (spriteBox is null)
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            return;
        }

        var spriteWidth = spriteBox.Value.Width;
        var spriteHeight = spriteBox.Value.Height;
        HitboxPreviewOffsetX = (HitboxViewGrid - spriteWidth) / 2.0;
        HitboxPreviewOffsetY = (HitboxViewGrid - spriteHeight) / 2.0;
    }

    #endregion
}
