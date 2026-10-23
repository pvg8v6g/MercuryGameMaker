namespace GameLibrary.Models.Actions;

public partial class MoneyAction : PageAction
{
    public int MoneyValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Change Money: {MoneyValue:+#;-#;0}";
}
