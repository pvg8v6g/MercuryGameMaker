namespace GameLibrary.Models.Actions;

public partial class SelectItemAction : PageAction
{
    /// <summary>Variable the selected item id is stored in.</summary>
    public string? Variable
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Select Item \u2192 {Variable}";
}
