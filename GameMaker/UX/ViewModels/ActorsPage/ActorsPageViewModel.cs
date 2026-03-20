using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;

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

    protected override ObservableCollection<Fighter> EntityCollection => gameDataService.Actors;

    public int CharacterIndex
    {
        get;
        set
        {
            if (SelectedEntity is null || string.IsNullOrEmpty(SelectedEntity.CharacterName))
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
        if (string.IsNullOrEmpty(fighter.CharacterName))
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

    protected override async Task LoadedAction()
    {
    }

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        OnPropertyChanged(nameof(CharacterIndex));

        var characterName = SelectedEntity?.CharacterName;
        if (SelectedEntity is null || string.IsNullOrEmpty(characterName))
        {
            CharacterIndex = 1;
            return;
        }

        var characterPath = Path.Combine(graphicsService.GetCharacterPath(), characterName);
        var divisions = (await graphicsService.GetSegmentation(characterPath)).width;
        CharacterIndex = (int) SelectedEntity.CharacterDirection * (int) divisions + SelectedEntity.CharacterIndex;
    }

    #endregion
}
