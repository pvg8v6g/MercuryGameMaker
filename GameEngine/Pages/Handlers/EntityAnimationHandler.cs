using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Systems;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Show Animation: plays the animation on the entity (or the player). With wait for completion the page holds until
/// every animation on it has played through.
/// </summary>
public class EntityAnimationHandler(IGameStateService gameStateService, AnimationSystem animationSystem) : ActionHandler<EntityAnimationAction>
{
    protected override ActionResult Start(PageRun run, EntityAnimationAction action)
    {
        if (Target(run, action) is not { } entity) return ActionResult.Done;

        animationSystem.Play(entity, action.AnimationId);
        return action.WaitForCompletion && animationSystem.IsPlaying(entity) ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, EntityAnimationAction action)
    {
        var entity = Target(run, action);
        return entity is not null && !entity.IsErased && animationSystem.IsPlaying(entity) ? ActionResult.Busy : ActionResult.Done;
    }

    private MapEntity? Target(PageRun run, EntityAnimationAction action)
    {
        return action.TargetsPlayer ? gameStateService.Player : gameStateService.Find(run, action.EntityId);
    }
}
