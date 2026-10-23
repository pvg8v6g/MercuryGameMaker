namespace GameLibrary.Models.Actions;

public partial class ChangeItemAction : PageAction
{
    public Guid? ItemId
    {
        get;
        set => SetField(ref field, value);
    }

    public int Amount
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Change Item: {Amount:+#;-#;0}";
}
