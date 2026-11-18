using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameLibrary.Models.Modules;

namespace GameEngine.Systems;

/// <summary>
/// Walks every entity with an input module in the direction being held.
/// </summary>
public class InputSystem(IInputService inputService, IGameStateService gameStateService, MovementSystem movementSystem) : IEntitySystem
{
    public void Update(MapEntity entity)
    {
        // a running page the player started (or an autorun) holds the player still
        if (gameStateService.IsPlayerBusy || !entity.HasModule<InputModule>() || inputService.Direction is not { } direction) return;
        movementSystem.Move(entity, direction);
    }
}
