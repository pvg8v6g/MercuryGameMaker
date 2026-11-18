using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Modules;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Branch Flow: runs the branch's actions when its condition passes, the else actions when it doesn't (if it has an
/// else). They run as a list of their own and the page carries on after the branch.
/// </summary>
public class BranchHandler(IGameStateService gameStateService) : ActionHandler<BranchAction>
{
    protected override ActionResult Start(PageRun run, BranchAction action)
    {
        var actions = Passed(run, action) ? action.BranchActions : action.ElseBranch ? action.ElseActions : null;
        if (actions is { Count: > 0 }) run.Lists.Push(new ActionList(actions));
        return ActionResult.Done;
    }

    // a switch, variable or self switch that was never set is off or empty. Conditions the engine doesn't keep yet
    // (timers, actor details, enemies, money) take the else branch
    private bool Passed(PageRun run, BranchAction action) => action.ConditionType switch
    {
        BranchConditionType.Switch => action.SwitchConditionName is { } name
                                      && gameStateService.Switches.GetValueOrDefault(name) == action.SwitchConditionValue,
        BranchConditionType.Variable => action.VariableConditionName is { } name
                                        && gameStateService.Variables.GetValueOrDefault(name, string.Empty) == (action.VariableConditionValue ?? string.Empty),
        BranchConditionType.SelfSwitch => action.SelfSwitchConditionKey is { } key && gameStateService.Find(run, action.SelfSwitchConditionEntity) is { } entity
                                          && gameStateService.SelfSwitch(entity, key) == action.SelfSwitchConditionValue,
        BranchConditionType.ActorInParty => action.ActorInPartyConditionActor is { } actor
                                            && gameStateService.ActiveParty.Contains(actor) == action.ActorInPartyConditionValue,
        BranchConditionType.EntityDirection => gameStateService.Find(run, action.EntityDirectionConditionEntity)?.GetModule<PositionModule>() is { } position
                                               && position.Direction == action.EntityDirectionConditionDirection,

        // items are owned or not, the count is 1 or 0
        BranchConditionType.PartyHasItem => action.PartyHasItemConditionItem is { } item
                                            && Compare(gameStateService.OwnedItems.Contains(item) ? 1 : 0, action.PartyHasItemConditionComparison, action.PartyHasItemConditionValue ?? 1),
        _ => false
    };

    private static bool Compare(int value, Comparison? comparison, int target) => comparison switch
    {
        Comparison.GreaterThan => value > target,
        Comparison.GreaterThanOrEquals => value >= target,
        Comparison.LessThan => value < target,
        Comparison.LessThanOrEquals => value <= target,
        _ => value == target
    };
}
