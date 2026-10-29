namespace GameLibrary.Models.Conditions;

public partial class LevelCondition : Condition
{
    public int Level
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public override string ToString() => $"Level >= {Level}";
}
