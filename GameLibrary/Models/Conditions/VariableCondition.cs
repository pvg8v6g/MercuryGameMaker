namespace GameLibrary.Models.Conditions;

public partial class VariableCondition : Condition
{
    public string? VariableName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Value
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Variable: {VariableName} == {Value}";
}
