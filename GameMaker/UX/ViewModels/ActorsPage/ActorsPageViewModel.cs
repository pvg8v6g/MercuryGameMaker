using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Models.Areas;
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

    public ObservableCollection<ActorElementModel> ElementResistanceStats { get; } = [];

    public ObservableCollection<EquipmentSlotModel> FighterEquipmentSlots { get; } = [];

    protected override ObservableCollection<Fighter> EntityCollection => gameDataService.Actors;


    private Direction _selectedDirection = Direction.Down;

    public Direction SelectedDirection
    {
        get => _selectedDirection;
        set
        {
            if (SetField(ref _selectedDirection, value))
            {
                LoadEditingHitboxes();
                OnPropertyChanged(nameof(PreviewHitboxes));
            }
        }
    }

    public ObservableCollection<Area> EditingHitboxes
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public ObservableCollection<Area> PreviewHitboxes
    {
        get
        {
            if (SelectedEntity == null) return [];
            return SelectedEntity.Hitboxes.GetValueOrDefault(SelectedDirection) ?? [];
        }
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
            OnPropertyChanged(nameof(PreviewHitboxes));
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
        EnsureHitboxes();
        SelectedDirection = SelectedEntity!.CharacterDirection;
        LoadEditingHitboxes();
        OnPropertyChanged(nameof(PreviewHitboxes));
        OnPropertyChanged(nameof(CharacterIndex));

        var characterName = SelectedEntity?.CharacterName;
        if (SelectedEntity is null || characterName.IsNullOrEmpty())
        {
            CharacterIndex = 1;
            return;
        }

        var characterPath = Path.Combine(graphicsService.GetCharacterPath(), characterName);
        var divisions = (await graphicsService.GetSegmentation(characterPath)).width;
        CharacterIndex = (int) SelectedEntity.CharacterDirection * (int) divisions + SelectedEntity.CharacterIndex;
    }

    #endregion

    #region Private Methods

    private void EnsureHitboxes()
    {
        if (SelectedEntity?.Hitboxes is null)
        {
            SelectedEntity?.Hitboxes = new Dictionary<Direction, ObservableCollection<Area>>();
        }
    }

    private void LoadEditingHitboxes()
    {
        EnsureHitboxes();

        if (SelectedEntity!.Hitboxes.TryGetValue(SelectedDirection, out var collection))
        {
            EditingHitboxes = collection;
        }
        else
        {
            collection = new ObservableCollection<Area>();
            SelectedEntity.Hitboxes[SelectedDirection] = collection;
            EditingHitboxes = collection;
        }
    }

    private void RefreshStats()
    {
        ElementResistanceStats.Clear();

        if (SelectedEntity is null) return;

        foreach (var element in GameDataService.Elements)
        {
            ElementResistanceStats.Add(new ActorElementModel(element, SelectedEntity));
        }

        OnPropertyChanged(nameof(ElementResistanceStats));
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

    #endregion
}
