using System.Collections.ObjectModel;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.AttributesPage;

public class AttributesPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Attribute>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Attribute> EntityCollection => gameDataService.Attributes;

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
    }

    #endregion
}
