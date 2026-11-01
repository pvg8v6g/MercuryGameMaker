using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Modules;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Services.Movement;

/// <summary>
/// Walks entities one step (grid size / division) at a time and animates the walk on the character sheet.
/// </summary>
public class MovementService(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    IModuleService moduleService) : IMovementService
{
    #region Public Methods

    public void UpdateCharacterMovement(MapEntity entity, MovementModule movementModule, PositionModule positionModule)
    {
        if (gameStateService.IsTransferring) return;
        DoMovement(entity, movementModule, positionModule);
    }

    public void Move(MapEntity entity, PositionModule positionModule, MovementModule movementModule, EntityHitboxModule? hitboxModule, Direction direction)
    {
        var movement = entity.Movement;
        movement.MoveInterrupted = false;
        if (movement.IsMoving) return;

        movement.DistanceX = 0.0;
        movement.DistanceY = 0.0;
        movement.IncrementX = 0.0;
        movement.IncrementY = 0.0;

        var disableDash = gameStateService.ActiveMap is { DisableDash: true };
        if (!movementModule.FixDirection) Turn(positionModule, direction);

        var div = disableDash ? 1.0 : inputService.Shift && movementModule.Dashing ? 2.0 : 1.0;
        var increment = div * movementModule.Speed;
        var distance = (double) gameDataService.GridSize / gameDataService.Division * gameDataService.ScaleFactor;

        switch (direction)
        {
            case Direction.Up:
                if (!ProcessCanWalk(entity, hitboxModule, positionModule, 0, -distance)) return;
                movement.IncrementY = -increment;
                movement.DistanceY = distance;
                break;
            case Direction.Down:
                if (!ProcessCanWalk(entity, hitboxModule, positionModule, 0, distance)) return;
                movement.IncrementY = increment;
                movement.DistanceY = distance;
                break;
            case Direction.Left:
                if (!ProcessCanWalk(entity, hitboxModule, positionModule, -distance, 0)) return;
                movement.IncrementX = -increment;
                movement.DistanceX = distance;
                break;
            case Direction.Right:
                if (!ProcessCanWalk(entity, hitboxModule, positionModule, distance, 0)) return;
                movement.IncrementX = increment;
                movement.DistanceX = distance;
                break;
            default:
                throw new ArgumentException($"Invalid direction: {direction}", nameof(direction));
        }
    }

    public void Turn(PositionModule positionModule, Direction direction)
    {
        positionModule.Direction = direction;
    }

    #endregion

    #region Private Methods

    private static void DoMovement(MapEntity entity, MovementModule movementModule, PositionModule positionModule)
    {
        var movement = entity.Movement;
        if (!movement.IsMoving)
        {
            movement.NotMovingTimer++;
            entity.Frame = 1;
            if (movement.NotMovingTimer <= 90) return;
            movement.SteppingTimer = 0;
            entity.SteppingIndex = 1;
            movement.NotMovingTimer = 0;
            return;
        }

        UpdateStepping(entity, movementModule);
        positionModule.X += movement.IncrementX;
        positionModule.Y += movement.IncrementY;
        movement.DistanceX -= Math.Abs(movement.IncrementX);
        movement.DistanceY -= Math.Abs(movement.IncrementY);
    }

    private static void UpdateStepping(MapEntity entity, MovementModule movementModule)
    {
        if (!movementModule.Stepping) return;
        entity.Movement.SteppingTimer++;
        if (entity.Movement.SteppingTimer < 15) return;
        UpdateSteppingIndex(entity);
    }

    // walks the frames left foot, standing, right foot, standing
    private static void UpdateSteppingIndex(MapEntity entity)
    {
        entity.SteppingIndex = entity.Frame switch
        {
            0 => 2,
            _ => 0
        };

        entity.Frame = entity.SteppingIndex;
        entity.Movement.SteppingTimer = 0;
    }

    private bool ProcessCanWalk(MapEntity entity, EntityHitboxModule? hitboxModule, PositionModule positionModule, double px, double py)
    {
        if (CanWalk(hitboxModule, positionModule, px, py)) return true;
        entity.Movement.MoveInterrupted = true;
        entity.Movement.WalkAroundInterrupted = true;
        return false;
    }

    private bool CanWalk(EntityHitboxModule? hitboxModule, PositionModule positionModule, double px, double py)
    {
        if (hitboxModule is null) return true;

        // where the hitboxes would be after the step
        var scale = gameDataService.ScaleFactor;
        var hitboxes = moduleService.GetAllHitboxes(hitboxModule, positionModule.Direction)
            .Select(area => new MapHitbox(area)
            {
                OriginX = positionModule.X + px,
                OriginY = positionModule.Y + py,
                OffsetX = area.OffsetX * scale,
                OffsetY = area.OffsetY * scale,
                Width = area.Width * scale,
                Height = area.Height * scale
            })
            .ToArray();

        if (MapOutOfBounds(hitboxes, positionModule)) return false;
        if (!hitboxModule.Collide) return true;
        CollideCharacter();
        return !EntityCollision(hitboxes);
    }

    // anything the step would bump into, ignoring the entity's own hitboxes
    private bool EntityCollision(MapHitbox[] hitboxes)
    {
        return moduleService.GetMapEntityHitboxes()
            .Where(x => hitboxes.All(y => y.Source != x.Source))
            .Any(x => hitboxes.Any(x.Collide));
    }

    // TODO - start page execution
    private static void CollideCharacter()
    {
    }

    // stops at the map edge and snaps the entity flush against it
    private bool MapOutOfBounds(MapHitbox[] hitboxes, PositionModule positionModule)
    {
        if (gameStateService.ActiveMap is not { } map) return false;

        var mapWidth = map.Width * gameDataService.GridSize * gameDataService.ScaleFactor;
        var mapHeight = map.Height * gameDataService.GridSize * gameDataService.ScaleFactor;
        foreach (var box in hitboxes)
        {
            if (box.X < 0)
            {
                positionModule.X = -box.OffsetX;
                return true;
            }

            if (box.Y < 0)
            {
                positionModule.Y = -box.OffsetY;
                return true;
            }

            if (box.X + box.Width > mapWidth)
            {
                positionModule.X = mapWidth - (box.OffsetX + box.Width);
                return true;
            }

            if (box.Y + box.Height > mapHeight)
            {
                positionModule.Y = mapHeight - (box.OffsetY + box.Height);
                return true;
            }
        }

        return false;
    }

    #endregion
}
