using GameEngine.Services.GameState;
using GameEngine.Systems;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Set Movement Route: puts the entity (the page's own when none is picked) on the route. With wait for completion the
/// page holds until the route is done. A repeating route never finishes, so the page never waits on one.
/// </summary>
public class EntityMovementHandler(IGameStateService gameStateService, MovementRouteSystem movementRouteSystem) : ActionHandler<EntityMovementAction>
{
    protected override ActionResult Start(PageRun run, EntityMovementAction action)
    {
        if (gameStateService.Find(run, action.EntityId) is not { } entity) return ActionResult.Done;

        movementRouteSystem.Start(entity, action);
        return action.WaitForCompletion && !action.Repeat ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, EntityMovementAction action)
    {
        var entity = gameStateService.Find(run, action.EntityId);
        return entity is not null && movementRouteSystem.IsFollowing(entity, action) ? ActionResult.Busy : ActionResult.Done;
    }
}
