using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Modules;

namespace GameEngine.Services.Modules;

/// <summary>
/// Reads modules across every entity on the active map.
/// </summary>
public class ModuleService(IGameStateService gameStateService) : IModuleService
{
    #region Read

    public IEnumerable<T> GetMapModules<T>() where T : Module
    {
        return gameStateService.MapEntities.SelectMany(x => x.Modules).OfType<T>();
    }

    // the hitboxes other entities can bump into
    public IEnumerable<MapHitbox> GetMapEntityHitboxes()
    {
        return EntityHitboxes(x => x.Collide);
    }

    // the hitboxes that are drawn for debugging
    public IEnumerable<MapHitbox> GetMapShownHitboxes()
    {
        return EntityHitboxes(x => x.ShowHitboxes);
    }

    // the hitboxes used in every direction plus the ones for the direction being faced
    public IEnumerable<Area> GetAllHitboxes(EntityHitboxModule hitboxModule, Direction direction)
    {
        var none = hitboxModule.Hitboxes.GetValueOrDefault(Direction.None) ?? [];
        if (direction == Direction.None) return none;
        var facing = hitboxModule.Hitboxes.GetValueOrDefault(direction) ?? [];
        return none.Concat(facing);
    }

    #endregion

    #region Private Methods

    private IEnumerable<MapHitbox> EntityHitboxes(Func<EntityHitboxModule, bool> include)
    {
        foreach (var entity in gameStateService.MapEntities)
        {
            if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule || !include(hitboxModule)) continue;
            if (entity.GetModule<PositionModule>() is not { } positionModule) continue;

            foreach (var area in GetAllHitboxes(hitboxModule, positionModule.Direction)) yield return entity.GetHitbox(area);
        }
    }

    #endregion
}
