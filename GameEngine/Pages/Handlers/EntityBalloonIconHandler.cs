using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Systems;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Show Balloon Icon: plays the balloon over the player or the entity (the page's own when none is picked). With wait for completion
/// the page holds until the balloon has played through and gone.
/// </summary>
public class EntityBalloonIconHandler(IGameStateService gameStateService, BalloonSystem balloonSystem) : ActionHandler<EntityBalloonIconAction>
{
    protected override ActionResult Start(PageRun run, EntityBalloonIconAction action)
    {
        if (Target(run, action) is not { } entity) return ActionResult.Done;

        balloonSystem.Show(entity, action.BalloonIconId);
        return action.WaitForCompletion && balloonSystem.IsShowing(entity) ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, EntityBalloonIconAction action)
    {
        var entity = Target(run, action);
        return entity is not null && balloonSystem.IsShowing(entity) ? ActionResult.Busy : ActionResult.Done;
    }

    private MapEntity? Target(PageRun run, EntityBalloonIconAction action)
    {
        return action.TargetsPlayer ? gameStateService.Player : gameStateService.Find(run, action.EntityId);
    }
}
