namespace GameLibrary.Models.Actions;

public partial class RecoverAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ClearStates
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public override string ToString() => "Recover All";
}
