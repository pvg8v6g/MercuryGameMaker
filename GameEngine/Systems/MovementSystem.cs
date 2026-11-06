using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Systems;

/// <summary>
/// Walks entities one step (grid size / division) at a time. A step only starts when nothing is in the way.
/// </summary>
public class MovementSystem(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    HitboxSystem hitboxSystem) : IEntitySystem
{
    #region Update

    public void Update(MapEntity entity)
    {
        if (entity.GetModule<PositionModule>() is not { } position) return;

        var movement = entity.State<MovementState>();
        movement.Stepped = movement.IsMoving;
        if (!movement.Stepped) return;

        // never past the end of the step, the distance is all that was checked to be clear
        var moveX = Math.Min(Math.Abs(movement.IncrementX), movement.DistanceX);
        var moveY = Math.Min(Math.Abs(movement.IncrementY), movement.DistanceY);
        position.X += Math.Sign(movement.IncrementX) * moveX;
        position.Y += Math.Sign(movement.IncrementY) * moveY;
        movement.DistanceX -= moveX;
        movement.DistanceY -= moveY;
    }

    #endregion

    #region Methods

    // turns the entity and starts a step, unless it is already mid step
    public void Move(MapEntity entity, Direction direction)
    {
        if (entity.GetModule<PositionModule>() is not { } position || entity.GetModule<MovementModule>() is not { } movementModule) return;

        var movement = entity.State<MovementState>();
        movement.MoveInterrupted = false;
        if (movement.IsMoving) return;

        movement.DistanceX = 0.0;
        movement.DistanceY = 0.0;
        movement.IncrementX = 0.0;
        movement.IncrementY = 0.0;

        if (!movementModule.FixDirection) position.Direction = direction;

        var dashing = gameStateService.ActiveMap is not { DisableDash: true } && inputService.Shift && movementModule.Dashing;
        var increment = (dashing ? 2.0 : 1.0) * movementModule.Speed;
        var distance = (double) gameDataService.GridSize / gameDataService.Division * gameDataService.ScaleFactor;

        var (unitX, unitY) = direction switch
        {
            Direction.Up => (0.0, -1.0),
            Direction.Down => (0.0, 1.0),
            Direction.Left => (-1.0, 0.0),
            Direction.Right => (1.0, 0.0),
            _ => throw new ArgumentException($"Invalid direction: {direction}", nameof(direction))
        };

        var reach = Reach(entity, position, unitX, unitY, Math.Max(distance, increment), distance);
        if (reach <= 0)
        {
            movement.MoveInterrupted = true;
            movement.WalkAroundInterrupted = true;
            return;
        }

        movement.IncrementX = unitX * increment;
        movement.IncrementY = unitY * increment;
        movement.DistanceX = Math.Abs(unitX) * reach;
        movement.DistanceY = Math.Abs(unitY) * reach;
    }

    #endregion

    #region Private Methods

    // how far this step can go. A dash covers more than one grid step in an update, so all of it is checked and it is
    // shortened to what fits: running at a wall stops against it instead of inside it, 0 when nothing fits
    private double Reach(MapEntity entity, PositionModule position, double unitX, double unitY, double farthest, double step)
    {
        for (var reach = farthest; reach > 0; reach -= step)
        {
            if (CanWalk(entity, position, unitX * reach, unitY * reach)) return reach;
        }

        return 0;
    }

    private bool CanWalk(MapEntity entity, PositionModule position, double stepX, double stepY)
    {
        if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule) return true;

        var hitboxes = hitboxSystem.Project(hitboxModule, position, stepX, stepY);
        if (MapEdge(hitboxes, position) is { } edge)
        {
            EdgeTransfer(entity, position, edge.Side, edge.Box);
            return false;
        }

        if (!hitboxModule.Collide) return true;

        // TODO - start page execution of whatever was walked into
        return !hitboxSystem.Collides(hitboxes);
    }

    // stops at the map edge and snaps the entity flush against it, the side is the edge that was walked into
    private (Direction Side, MapHitbox Box)? MapEdge(MapHitbox[] hitboxes, PositionModule position)
    {
        if (gameStateService.ActiveMap is not { } map) return null;

        var (mapWidth, mapHeight) = MapSize(map);
        foreach (var box in hitboxes)
        {
            if (box.X < 0)
            {
                position.X = -box.OffsetX;
                return (Direction.Left, box);
            }

            if (box.Y < 0)
            {
                position.Y = -box.OffsetY;
                return (Direction.Up, box);
            }

            if (box.X + box.Width > mapWidth)
            {
                position.X = mapWidth - (box.OffsetX + box.Width);
                return (Direction.Right, box);
            }

            if (box.Y + box.Height > mapHeight)
            {
                position.Y = mapHeight - (box.OffsetY + box.Height);
                return (Direction.Down, box);
            }
        }

        return null;
    }

    // the player walking into an edge with a connected map comes in through the opposite edge of that map, flush
    // against it, and keeps its place along the edge (pulled in when the new map is smaller)
    private void EdgeTransfer(MapEntity entity, PositionModule position, Direction side, MapHitbox box)
    {
        if (entity != gameStateService.Player || gameStateService.ActiveMap is not { } map) return;

        var connection = side switch
        {
            Direction.Up => map.North,
            Direction.Down => map.South,
            Direction.Left => map.West,
            Direction.Right => map.East,
            _ => null
        };
        if (connection is null || gameDataService.GameMaps.FirstOrDefault(x => x.Guid == connection && !x.FolderOnly) is not { } target) return;

        var (width, height) = MapSize(target);
        var left = -box.OffsetX;
        var right = width - (box.OffsetX + box.Width);
        var top = -box.OffsetY;
        var bottom = height - (box.OffsetY + box.Height);
        var x = side switch
        {
            Direction.Left => right,
            Direction.Right => left,
            _ => Math.Max(left, Math.Min(position.X, right))
        };
        var y = side switch
        {
            Direction.Up => bottom,
            Direction.Down => top,
            _ => Math.Max(top, Math.Min(position.Y, bottom))
        };

        var scale = gameDataService.ScaleFactor;
        gameStateService.EdgeTransfer = new EdgeTransfer(target, x / scale, y / scale, side);
    }

    // in scaled pixels, what the game plays in
    private (double Width, double Height) MapSize(GameMap map)
    {
        var tile = gameDataService.GridSize * gameDataService.ScaleFactor;
        return (map.Width * tile, map.Height * tile);
    }

    #endregion
}
