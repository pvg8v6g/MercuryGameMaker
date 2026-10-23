namespace GameLibrary.Models.Actions;

public partial class InputNumberAction : PageAction
{
    /// <summary>Variable the entered number is stored in.</summary>
    public string? Variable
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Input Number \u2192 {Variable}";
}
