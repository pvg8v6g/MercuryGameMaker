namespace GameLibrary.Models.Actions;

public partial class SaveAction : PageAction
{
    public Guid? AnimationId
    {
        get;
        set => SetField(ref field, value);
    }

    public bool RecoverParty
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Save Game";
}
