using System.Collections.ObjectModel;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.MaterialsPage;

/// <summary>
/// The Materials page: the basics every item has (name, icon, cost, sort, description). More options get added when the
/// crafting system is designed.
/// </summary>
public class MaterialsPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Material>(jsonService)
{
    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Material> EntityCollection => gameDataService.Materials;
}
