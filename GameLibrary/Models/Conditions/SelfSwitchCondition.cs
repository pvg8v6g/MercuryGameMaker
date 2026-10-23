using GameLibrary.Enumerations;

namespace GameLibrary.Models.Conditions;

public partial class SelfSwitchCondition : Condition
{
    public SelfSwitch? SelfSwitch
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Value
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Self Switch: {SelfSwitch} == {Value}";
}
