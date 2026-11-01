using GameEngine.Models;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;

namespace GameEngine.Services.Movement;

public interface IMovementService
{
    void UpdateCharacterMovement(MapEntity entity, MovementModule movementModule, PositionModule positionModule);

    void Move(MapEntity entity, PositionModule positionModule, MovementModule movementModule, EntityHitboxModule? hitboxModule, Direction direction);

    void Turn(PositionModule positionModule, Direction direction);
}
