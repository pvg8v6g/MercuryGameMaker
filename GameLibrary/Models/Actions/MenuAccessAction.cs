namespace GameLibrary.Models.Actions;

public partial class MenuAccessAction : PageAction
{
    public bool Disable
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Menu Access: {(Disable ? "Disable" : "Enable")}";
}
