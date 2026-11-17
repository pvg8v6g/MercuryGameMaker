using GameLibrary.Models;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using GameMaker.Services.Navigation;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.ViewModels.Popups.MapIdConflict;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Maps;

public class MapEditService(IGameDataService gameDataService, IJsonService jsonService, INavigationService navigationService) : IMapEditService
{
    #region Methods

    public GameMap Create(bool folder, Guid? parent)
    {
        var map = new GameMap
        {
            Id = Calculations.GetNextId([.. gameDataService.GameMaps.Select(x => x.Id)]),
            Name = folder ? "New Folder" : "New Map",
            FolderOnly = folder,
            ParentGuid = parent
        };

        gameDataService.GameMaps.Add(map);
        return map;
    }

    public GameMap? Duplicate(GameMap map)
    {
        if (jsonService.Clone(map) is not { } clone) return null;

        clone.Guid = Guid.NewGuid();
        clone.Id = map.Id + 1;
        clone.IsExpanded = false;
        foreach (var entity in clone.Entities) ReassignGuids(entity, clone.Guid);
        foreach (var area in clone.Areas) area.Guid = Guid.NewGuid();

        gameDataService.GameMaps.Add(clone);
        return clone;
    }

    public void Delete(GameMap map)
    {
        foreach (var other in gameDataService.GameMaps)
        {
            if (other.ParentGuid == map.Guid) other.ParentGuid = map.ParentGuid;
            if (other.North == map.Guid) other.North = null;
            if (other.East == map.Guid) other.East = null;
            if (other.South == map.Guid) other.South = null;
            if (other.West == map.Guid) other.West = null;
        }

        gameDataService.GameMaps.Remove(map);
    }

    public async Task<bool> ResolveIdConflict(GameMap map)
    {
        if (gameDataService.GameMaps.FirstOrDefault(x => !Equals(x, map) && x.Id == map.Id) is not { } other) return true;

        var otherId = other.Id;
        var viewModel = new MapIdConflictViewModel(new MapIdModel(map, gameDataService.GameMaps), new MapIdModel(other, gameDataService.GameMaps));
        while (true)
        {
            var result = await navigationService.ShowMapIdConflictPopup(viewModel);
            if (result != ContentDialogResult.Primary)
            {
                other.Id = otherId;
                return false;
            }

            if (!viewModel.HasConflict) return true;
        }
    }

    public void ReassignGuids(Entity entity, Guid mapGuid)
    {
        entity.Guid = Guid.NewGuid();
        entity.MapId = mapGuid;
        foreach (var page in entity.Pages)
        {
            page.Guid = Guid.NewGuid();
            page.EntityId = entity.Guid;
        }
    }

    #endregion
}
