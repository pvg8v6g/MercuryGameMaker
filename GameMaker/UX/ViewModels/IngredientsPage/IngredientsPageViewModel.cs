using System.Collections.ObjectModel;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.IngredientsPage;

/// <summary>
/// The Ingredients page: the basics every item has (name, icon, cost, sort, description). More options get added when the
/// cooking system is designed.
/// </summary>
public class IngredientsPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Ingredient>(jsonService)
{
    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Ingredient> EntityCollection => gameDataService.Ingredients;
}
