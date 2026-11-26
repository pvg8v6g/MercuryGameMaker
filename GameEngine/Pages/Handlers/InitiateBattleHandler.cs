using GameEngine.Models.Battle;
using GameEngine.Services.GameState;
using GameLibrary.Models.Actions;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Troops;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Initiate Battle: fights the troop (or, for a random battle, one of its troops picked at random) on its battle map. The scene fades out, loads the battle and later brings the
/// player back to the exact spot they left. The page waits for all of that and then carries on, without holding the
/// player still while they fight. A troop that's missing, or has no battle map, is skipped.
/// </summary>
public class InitiateBattleHandler(IGameDataService gameDataService, IGameStateService gameStateService) : ActionHandler<InitiateBattleAction>
{
    protected override ActionResult Start(PageRun run, InitiateBattleAction action)
    {
        // a battle can't start inside another one
        if (gameStateService.Battle is not null || gameStateService.BattleRequest is not null) return ActionResult.Done;
        if (PickTroop(action) is not { } pick) return ActionResult.Done;

        gameStateService.BattleRequest = new BattleRequest(pick.troop, pick.battleMap);
        run.WaitsForBattle = true;
        return ActionResult.Busy;
    }

    // the one troop, or a random one of the ticked troops that can be fought (still there, with a battle map)
    private (Troop troop, GameMap battleMap)? PickTroop(InitiateBattleAction action)
    {
        List<Guid> ids = action.RandomTroop ? action.RandomTroopIds : action.TroopId is { } id ? [id] : [];
        var fightable = new List<(Troop, GameMap)>();
        foreach (var troopId in ids)
        {
            if (gameDataService.Troops.FirstOrDefault(x => x.Guid == troopId) is not { } troop) continue;
            if (gameDataService.GameMaps.FirstOrDefault(x => x.Guid == troop.BattleMapId) is not { } battleMap) continue;
            fightable.Add((troop, battleMap));
        }

        return fightable.Count == 0 ? null : fightable[Random.Shared.Next(fightable.Count)];
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
