using GameEngine.Models;
using GameEngine.Services.GameState;

namespace GameEngine.Pages;

/// <summary>
/// Finds the entity an action points at. An action that names no entity means the page's own.
/// </summary>
public static class PageEntities
{
    // by the entity's guid
    public static MapEntity? Find(this IGameStateService gameStateService, PageRun run, Guid? guid)
    {
        return guid is { } id ? gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Guid == id) : run.Entity;
    }

    // by the entity's id on the map
    public static MapEntity? Find(this IGameStateService gameStateService, PageRun run, int? id)
    {
        return id is { } mapId ? gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Id == mapId) : run.Entity;
    }
}
