using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ChangeItemAction : PageAction
{
    public Guid? ItemId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ItemName
    {
        get;
        set => SetField(ref field, value);
    }

    public int Amount
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Change Item";
        if (!ItemName.IsNullOrEmpty()) result += $" {ItemName}";
        result += $": {Amount:+#;-#;0}";
        return result;
    }
}
