using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Systems;

/// <summary>
/// Moves every entity's hitboxes onto it, scaled to the map, and answers what they touch.
/// An entity uses its hitboxes for every direction plus the ones for the direction it faces.
/// </summary>
public class HitboxSystem(IGameDataService gameDataService, IGameStateService gameStateService) : IEntitySystem
{
    #region Update

    // only puts the hitboxes where the entities stand (nothing is hit here), the camera centers on them, so a paused
    // map that was just loaded still has its camera on the player
    public bool RunsWhilePaused => true;

    public void Update(MapEntity entity)
    {
        if (entity.GetModule<PositionModule>() is not { } position || entity.GetModule<EntityHitboxModule>() is not { } hitboxModule) return;

        var state = entity.State<HitboxState>();
        foreach (var area in Areas(hitboxModule, position.Direction)) Place(state.Get(area), area, position, 0, 0);
    }

    #endregion

    #region Methods

    // where the entity's hitboxes would be after moving by stepX, stepY
    public MapHitbox[] Project(EntityHitboxModule hitboxModule, PositionModule position, double stepX, double stepY)
    {
        return Areas(hitboxModule, position.Direction).Select(area => Place(new MapHitbox(area), area, position, stepX, stepY)).ToArray();
    }

    // whether a collision area or any other entity that collides is in the way of the hitboxes
    public bool Collides(MapHitbox[] hitboxes)
    {
        if (HitsCollisionArea(hitboxes)) return true;

        return Placed(x => x.Collide)
            .Where(x => hitboxes.All(y => y.Source != x.Source))
            .Any(x => hitboxes.Any(x.Collide));
    }

    // the hitbox an entity is centered on, the first one used in every direction
    public MapHitbox? Main(MapEntity entity)
    {
        if (entity.GetModule<EntityHitboxModule>()?.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return null;
        return entity.State<HitboxState>().Get(area);
    }

    // every entity's hitboxes where they are right now
    public IEnumerable<MapHitbox> AllPlaced() => Placed(_ => true);

    #endregion

    #region Private Methods

    private static IEnumerable<Area> Areas(EntityHitboxModule hitboxModule, Direction direction)
    {
        var none = hitboxModule.Hitboxes.GetValueOrDefault(Direction.None) ?? [];
        if (direction == Direction.None) return none;
        return none.Concat(hitboxModule.Hitboxes.GetValueOrDefault(direction) ?? []);
    }

    private MapHitbox Place(MapHitbox hitbox, Area area, PositionModule position, double stepX, double stepY)
    {
        var scale = gameDataService.ScaleFactor;
        hitbox.OriginX = position.X + stepX;
        hitbox.OriginY = position.Y + stepY;
        hitbox.OffsetX = area.OffsetX * scale;
        hitbox.OffsetY = area.OffsetY * scale;
        hitbox.Width = area.Width * scale;
        hitbox.Height = area.Height * scale;
        hitbox.Z = position.Z + position.SortZ + 1;
        return hitbox;
    }

    // the collision areas painted on the map in the editor, saved in unscaled map pixels like entity positions
    private bool HitsCollisionArea(MapHitbox[] hitboxes)
    {
        var scale = gameDataService.ScaleFactor;
        foreach (var area in gameStateService.MapAreas)
        {
            if (area.AreaType != MapAreaType.Collision) continue;

            var x = area.OffsetX * scale;
            var y = area.OffsetY * scale;
            var width = area.Width * scale;
            var height = area.Height * scale;
            if (hitboxes.Any(box => box.X < x + width && x < box.X + box.Width && box.Y < y + height && y < box.Y + box.Height)) return true;
        }

        return false;
    }

    // the placed hitboxes of every entity whose hitbox module matches
    private IEnumerable<MapHitbox> Placed(Func<EntityHitboxModule, bool> include)
    {
        foreach (var entity in gameStateService.MapEntities)
        {
            if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule || !include(hitboxModule)) continue;
            if (entity.GetModule<PositionModule>() is not { } position) continue;

            var state = entity.State<HitboxState>();
            foreach (var area in Areas(hitboxModule, position.Direction)) yield return state.Get(area);
        }
    }

    #endregion
}
