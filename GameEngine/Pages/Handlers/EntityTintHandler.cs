using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Models.Actions;
using GameLibrary.Services.GameData;
using GameLibrary.Utilities;
using Microsoft.UI;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Entity Tint: fades a color over the entity (or the player) to the strength over the fade time, 0 fades it out.
/// With wait for completion the page holds until the fade is done.
/// </summary>
public class EntityTintHandler(IGameDataService gameDataService, IGameStateService gameStateService) : ActionHandler<EntityTintAction>
{
    protected override ActionResult Start(PageRun run, EntityTintAction action)
    {
        if (Target(run, action) is not { } entity) return ActionResult.Done;

        var appearance = entity.State<AppearanceState>();
        appearance.Tint.To(action.Color.ToColorOr(Colors.White), action.Strength, action.BlendMode, gameDataService.Updates(action.FadeTime));
        return action.WaitForCompletion && appearance.Tint.IsFading ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, EntityTintAction action)
    {
        return Target(run, action) is { IsErased: false } entity && entity.State<AppearanceState>().Tint.IsFading ? ActionResult.Busy : ActionResult.Done;
    }

    private MapEntity? Target(PageRun run, EntityTintAction action)
    {
        return action.TargetsPlayer ? gameStateService.Player : gameStateService.Find(run, action.EntityId);
    }
}
