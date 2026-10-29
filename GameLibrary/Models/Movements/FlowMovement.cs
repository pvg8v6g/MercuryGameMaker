using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

/// <summary>
/// Controls flow during a movement route by toggling a switch, variable,
/// self switch or timer. Only the populated control is applied.
/// </summary>
public partial class FlowMovement : Movement
{
    public string? SwitchName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool? SwitchValue
    {
        get;
        set => SetField(ref field, value);
    }

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

    public string? SelfSwitchValue
    {
        get;
        set => SetField(ref field, value);
    }

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

    public override string ToString() => "Flow Control";
}
