using GameEngine.Models.Battle;
using GameEngine.Services.GameState;
using GameLibrary.Models.Actions;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Initiate Battle: fights the troop on its battle map. The scene fades out, loads the battle and later brings the
/// player back to the exact spot they left. The page waits for all of that and then carries on, without holding the
/// player still while they fight. A troop that's missing, or has no battle map, is skipped.
/// </summary>
public class InitiateBattleHandler(IGameDataService gameDataService, IGameStateService gameStateService) : ActionHandler<InitiateBattleAction>
{
    protected override ActionResult Start(PageRun run, InitiateBattleAction action)
    {
        // a battle can't start inside another one
        if (gameStateService.Battle is not null || gameStateService.BattleRequest is not null) return ActionResult.Done;
        if (gameDataService.Troops.FirstOrDefault(x => x.Guid == action.TroopId) is not { } troop) return ActionResult.Done;
        if (gameDataService.GameMaps.FirstOrDefault(x => x.Guid == troop.BattleMapId) is not { } battleMap) return ActionResult.Done;

        gameStateService.BattleRequest = new BattleRequest(troop, battleMap);
        run.WaitsForBattle = true;
        return ActionResult.Busy;
    }

    // done once the battle is over and the player is standing on the story map again
    protected override ActionResult Update(PageRun run, InitiateBattleAction action)
    {
        var fighting = gameStateService.BattleRequest is not null || gameStateService.Battle is not null || gameStateService.EdgeTransfer is not null || gameStateService.IsTransferring;
        if (fighting) return ActionResult.Busy;

        run.WaitsForBattle = false;
        return ActionResult.Done;
    }
}
