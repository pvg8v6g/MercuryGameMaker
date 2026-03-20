using System.Collections.ObjectModel;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.StatesPage;

public class StatesPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<State>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<State> EntityCollection => gameDataService.States;

    #endregion
}
