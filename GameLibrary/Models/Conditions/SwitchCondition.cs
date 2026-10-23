namespace GameLibrary.Models.Conditions;

public partial class SwitchCondition : Condition
{
    public string? SwitchName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Value
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Switch: {SwitchName} == {Value}";
}
