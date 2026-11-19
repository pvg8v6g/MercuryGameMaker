using GameEngine.Services.GameState;
using GameEngine.Services.Party;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Change Money: adds the amount (a negative amount takes it away), the party never has less than nothing.
/// </summary>
public class MoneyHandler(IGameStateService gameStateService) : ActionHandler<MoneyAction>
{
    protected override ActionResult Start(PageRun run, MoneyAction action)
    {
        gameStateService.Money = Math.Max(0, gameStateService.Money + action.MoneyValue);
        return ActionResult.Done;
    }
}

/// <summary>
/// Change Items: adds the amount of the item (a negative amount takes them away).
/// </summary>
public class ChangeItemHandler(PartyService partyService) : ActionHandler<ChangeItemAction>
{
    protected override ActionResult Start(PageRun run, ChangeItemAction action)
    {
        if (action.ItemId is { } item) partyService.ChangeItem(item, action.Amount);
        return ActionResult.Done;
    }
}

/// <summary>
/// Change Party: adds the actor to the end of the party (initialize: as it was made in the editor) or takes it out.
/// The first in the party is the leader the player walks around as.
/// </summary>
public class PartyHandler(PartyService partyService) : ActionHandler<PartyAction>
{
    protected override ActionResult Start(PageRun run, PartyAction action)
    {
        if (action.ActorId is not { } actor) return ActionResult.Done;

        if (action.Add) partyService.AddToParty(actor, action.Initialize);
        else partyService.RemoveFromParty(actor);
        return ActionResult.Done;
    }
}

/// <summary>
/// Recover All: full life, mana and shield for the actor (or the whole party when none is picked), and its states
/// cleared when asked.
/// </summary>
public class RecoverHandler(PartyService partyService) : ActionHandler<RecoverAction>
{
    protected override ActionResult Start(PageRun run, RecoverAction action)
    {
        foreach (var actor in partyService.Targets(action.ActorId))
        {
            actor.Life = actor.MaxLife;
            actor.Mana = actor.MaxMana;
            actor.Shield = actor.MaxShield;
            if (action.ClearStates) actor.States.Clear();
        }

        return ActionResult.Done;
    }
}
