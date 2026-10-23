namespace GameLibrary.Models.Actions;

public partial class MenuAction : PageAction
{
    public bool DisableExit
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Open Menu";
}
