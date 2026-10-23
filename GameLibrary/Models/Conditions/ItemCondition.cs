namespace GameLibrary.Models.Conditions;

public partial class ItemCondition : Condition
{
    public Guid? OwnItem
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? DoNotOwnItem
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() =>
        OwnItem != null ? "Owns Item" :
        DoNotOwnItem != null ? "Does Not Own Item" :
        "Item Condition";
}
