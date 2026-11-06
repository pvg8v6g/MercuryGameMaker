using GameEngine.Models;
using GameEngine.Services.Input;
using GameLibrary.Models.Modules;

namespace GameEngine.Systems;

/// <summary>
/// Walks every entity with an input module in the direction being held.
/// </summary>
public class InputSystem(IInputService inputService, MovementSystem movementSystem) : IEntitySystem
{
    public void Update(MapEntity entity)
    {
        if (!entity.HasModule<InputModule>() || inputService.Direction is not { } direction) return;
        movementSystem.Move(entity, direction);
    }
}
