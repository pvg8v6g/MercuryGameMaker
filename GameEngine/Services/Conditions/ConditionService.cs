using GameEngine.Services.GameState;
using GameEngine.Models;
using GameLibrary.Models.Conditions;

namespace GameEngine.Services.Conditions;

/// <summary>
/// Checks a page condition against the game state. Self switches are checked against the entity the page belongs to.
/// </summary>
public class ConditionService(IGameStateService gameStateService) : IConditionService
{
    #region Methods

    public bool Passed(Condition condition, MapEntity entity) => condition switch
    {
        SelfSwitchCondition selfSwitch => selfSwitch.SelfSwitch is { } key && selfSwitch.Value == gameStateService.SelfSwitch(entity, key),

        // a switch or variable that was never set doesn't block the page
        SwitchCondition @switch => @switch.SwitchName is { } name && (!gameStateService.Switches.TryGetValue(name, out var value) || value == @switch.Value),
        VariableCondition variable => variable.VariableName is { } name && (!gameStateService.Variables.TryGetValue(name, out var value) || value == variable.Value),

        ItemCondition item => Owns(gameStateService.OwnedItems, item.OwnItem, item.DoNotOwnItem),
        ActorCondition actor => Owns(gameStateService.OwnedParty, actor.OwnActor, actor.DoNotOwnActor),

        // party levels aren't tracked by the engine yet
        LevelCondition => true,

        _ => false
    };

    #endregion

    #region Private Methods

    private static bool Owns(HashSet<Guid> owned, Guid? own, Guid? doNotOwn)
    {
        var result = true;
        if (own is { } ownGuid) result = owned.Contains(ownGuid);
        if (doNotOwn is { } doNotOwnGuid) result = !owned.Contains(doNotOwnGuid);
        return result;
    }

    #endregion
}
