using GameEngine.Services.GameState;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Erase Entity: takes the entity off the map until the map loads again. Its page still finishes.
/// </summary>
public class EntityEraseHandler(IGameStateService gameStateService) : ActionHandler<EntityEraseAction>
{
    protected override ActionResult Start(PageRun run, EntityEraseAction action)
    {
        if (gameStateService.Find(run, action.EntityId) is { } entity) entity.IsErased = true;
        return ActionResult.Done;
    }
}
