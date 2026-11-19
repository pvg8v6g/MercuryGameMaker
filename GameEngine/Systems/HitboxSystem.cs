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
/// An entity's level is its Z: it only bumps into areas and entities on the same level.
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

    // whether a collision area or any other entity that collides is in the way of moving the hitboxes to where they're
    // projected, both on the entity's level
    public bool Collides(PositionModule position, MapHitbox[] current, MapHitbox[] projected, Direction direction)
    {
        var level = Level(position);
        if (HitsCollisionArea(current, projected, direction, level)) return true;

        return Placed(x => x.Collide, level)
            .Where(x => projected.All(y => y.Source != x.Source))
            .Any(x => projected.Any(x.Collide));
    }

    // a level change area the entity's main hitbox (its feet) stands in moves it to the area's level
    public void ChangeLevel(EntityHitboxModule hitboxModule, PositionModule position)
    {
        if (hitboxModule.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return;

        var feet = Place(new MapHitbox(area), area, position, 0, 0);
        var change = gameStateService.MapAreas.FirstOrDefault(x => x.AreaType == MapAreaType.LevelChange && Overlaps(feet, x));
        if (change is not null) position.Z = change.Level;
    }

    // the level a level change area under the feet moves an entity to, none when the feet aren't on one
    public int? LevelChangeAt(MapHitbox feet)
    {
        return gameStateService.MapAreas.FirstOrDefault(x => x.AreaType == MapAreaType.LevelChange && Overlaps(feet, x))?.Level;
    }

    // the hitboxes an entity would have standing at x, y (scaled map pixels) facing a direction
    public MapHitbox[] PlaceAt(EntityHitboxModule hitboxModule, double x, double y, Direction direction)
    {
        var scale = gameDataService.ScaleFactor;
        return Areas(hitboxModule, direction).Select(area => new MapHitbox(area)
        {
            OriginX = x,
            OriginY = y,
            OffsetX = area.OffsetX * scale,
            OffsetY = area.OffsetY * scale,
            Width = area.Width * scale,
            Height = area.Height * scale
        }).ToArray();
    }

    // the draw tier: the entity's own level when its feet are on a surface of a higher level (underneath a bridge),
    // otherwise the top tier with everything else on the map
    public int Tier(MapEntity entity, PositionModule position)
    {
        if (entity.GetModule<EntityHitboxModule>()?.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return SceneSprite.TopTier;

        var level = Level(position);
        var feet = Place(new MapHitbox(area), area, position, 0, 0);
        return gameStateService.MapAreas.Any(x => x.IsSurface && x.Level > level && Overlaps(feet, x)) ? level : SceneSprite.TopTier;
    }

    // puts the entity on the level of the highest surface its feet are on, the ground when they aren't on one
    public void StandOnSurface(MapEntity entity)
    {
        if (entity.GetModule<PositionModule>() is not { } position) return;
        if (entity.GetModule<EntityHitboxModule>()?.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return;

        var feet = Place(new MapHitbox(area), area, position, 0, 0);
        position.Z = gameStateService.MapAreas.Where(x => x.IsSurface && Overlaps(feet, x)).Select(x => x.Level).DefaultIfEmpty(0).Max();
    }

    // the level an entity is on, its Z
    public static int Level(PositionModule position) => (int) Math.Round(position.Z);

    // the hitbox an entity is centered on, the first one used in every direction
    public MapHitbox? Main(MapEntity entity)
    {
        if (entity.GetModule<EntityHitboxModule>()?.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return null;
        return entity.State<HitboxState>().Get(area);
    }

    // from the middle of one entity's feet to the other's (their main hitboxes), their positions when either has none.
    // Feet, not pictures: a tall character's picture starts a tile higher than a short one's
    public (double X, double Y) Between(MapEntity from, MapEntity to)
    {
        if (Main(from) is { } a && Main(to) is { } b) return (b.X + (b.Width / 2.0) - (a.X + (a.Width / 2.0)), b.Y + (b.Height / 2.0) - (a.Y + (a.Height / 2.0)));

        if (from.GetModule<PositionModule>() is not { } start || to.GetModule<PositionModule>() is not { } end) return (0, 0);
        return (end.X - start.X, end.Y - start.Y);
    }

    // the way from one entity to the other along whichever way the other is farther, none when they're on the same spot
    public Direction Toward(MapEntity from, MapEntity to)
    {
        var (dx, dy) = Between(from, to);
        if (dx == 0 && dy == 0) return Direction.None;

        return Math.Abs(dx) > Math.Abs(dy)
            ? dx > 0 ? Direction.Right : Direction.Left
            : dy > 0 ? Direction.Down : Direction.Up;
    }

    // every entity's hitboxes where they are right now
    public IEnumerable<MapHitbox> AllPlaced() => Placed(_ => true);

    // one entity's hitboxes where it is right now
    public IEnumerable<MapHitbox> PlacedOf(MapEntity entity)
    {
        if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule || entity.GetModule<PositionModule>() is not { } position) return [];

        var state = entity.State<HitboxState>();
        return Areas(hitboxModule, position.Direction).Select(state.Get);
    }

    // the entities on the mover's level that the step newly touches: overlapped where it's going and not where it is.
    // An entity in the way is never overlapped, so walking into it again touches it again
    public MapEntity[] Touched(MapEntity mover, PositionModule position, MapHitbox[] current, MapHitbox[] projected)
    {
        var level = Level(position);
        return gameStateService.MapEntities
            .Where(x => x != mover && !x.IsErased && x.GetModule<PositionModule>() is { } other && Level(other) == level)
            .Where(x =>
            {
                var boxes = PlacedOf(x).ToArray();
                return boxes.Any(box => projected.Any(box.Collide)) && !boxes.Any(box => current.Any(box.Collide));
            })
            .ToArray();
    }

    #endregion

    #region Private Methods

    // the hitboxes used facing a direction: the ones for every direction plus that direction's
    public static IEnumerable<Area> Areas(EntityHitboxModule hitboxModule, Direction direction)
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

    // the collision areas painted on the map in the editor and the prop footprints. One only stops moving onto it in the
    // directions it blocks, an entity already standing on it moves freely, so a raised platform can be left from inside
    public bool HitsCollisionArea(MapHitbox[] current, MapHitbox[] projected, Direction direction, int level)
    {
        foreach (var area in gameStateService.MapAreas)
        {
            if (area.AreaType != MapAreaType.Collision || area.Level != level || !area.Blocks(direction)) continue;
            if (current.Any(box => Overlaps(box, area))) continue;
            if (projected.Any(box => Overlaps(box, area))) return true;
        }

        return false;
    }

    // map areas are saved in unscaled map pixels like entity positions
    public bool Overlaps(MapHitbox box, MapArea area)
    {
        var scale = gameDataService.ScaleFactor;
        return box.Collide(area.OffsetX * scale, area.OffsetY * scale, area.Width * scale, area.Height * scale);
    }

    // the placed hitboxes of every entity whose hitbox module matches, only the ones on the level when one is given
    private IEnumerable<MapHitbox> Placed(Func<EntityHitboxModule, bool> include, int? level = null)
    {
        foreach (var entity in gameStateService.MapEntities)
        {
            if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule || !include(hitboxModule)) continue;
            if (entity.GetModule<PositionModule>() is not { } position) continue;
            if (level is { } onLevel && Level(position) != onLevel) continue;

            var state = entity.State<HitboxState>();
            foreach (var area in Areas(hitboxModule, position.Direction)) yield return state.Get(area);
        }
    }

    #endregion
}
