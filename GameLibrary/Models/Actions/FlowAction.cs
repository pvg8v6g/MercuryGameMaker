using GameLibrary.Enumerations;

namespace GameLibrary.Models.Actions;

/// <summary>
/// Controls flow by toggling a switch, variable, self switch or timer.
/// Only the populated control is applied.
/// </summary>
public partial class FlowAction : PageAction
{
    public FlowConditionType ConditionType
    {
        get;
        set => SetField(ref field, value);
    }

    public string? SwitchName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool SwitchValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public string? VariableName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? VariableValue
    {
        get;
        set => SetField(ref field, value);
    }

    public int? SelfSwitchEntity
    {
        get;
        set => SetField(ref field, value);
    }

    public SelfSwitch? SelfSwitchKey
    {
        get;
        set => SetField(ref field, value);
    }

    public bool SelfSwitchValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public string? TimerName
    {
        get;
        set => SetField(ref field, value);
    }

    public int? TimerValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Control Flow\n    " + ConditionType switch
    {
        FlowConditionType.Switch => $"Switch {SwitchName} = {(SwitchValue ? "ON" : "OFF")}",
        FlowConditionType.Variable => $"Variable {VariableName} = {VariableValue}",
        FlowConditionType.SelfSwitch => $"Self Switch {SelfSwitchKey} = {(SelfSwitchValue ? "ON" : "OFF")}",
        FlowConditionType.Timer => $"Timer {TimerName} = {TimerValue}",
        _ => ConditionType.ToString()
    };
}
