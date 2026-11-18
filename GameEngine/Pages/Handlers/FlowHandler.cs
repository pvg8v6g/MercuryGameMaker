using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Control Flow: sets a switch, a variable or a self switch. Timers aren't kept by the engine yet.
/// </summary>
public class FlowHandler(IGameStateService gameStateService) : ActionHandler<FlowAction>
{
    protected override ActionResult Start(PageRun run, FlowAction action)
    {
        switch (action.ConditionType)
        {
            case FlowConditionType.Switch when !string.IsNullOrEmpty(action.SwitchName):
                gameStateService.Switches[action.SwitchName] = action.SwitchValue;
                break;

            case FlowConditionType.Variable when !string.IsNullOrEmpty(action.VariableName):
                gameStateService.Variables[action.VariableName] = action.VariableValue ?? string.Empty;
                break;

            // the entity is picked by its id on the map
            case FlowConditionType.SelfSwitch when action.SelfSwitchKey is { } key:
                if (gameStateService.Find(run, action.SelfSwitchEntity) is { } entity) gameStateService.SetSelfSwitch(entity, key, action.SelfSwitchValue);
                break;
        }

        return ActionResult.Done;
    }
}
