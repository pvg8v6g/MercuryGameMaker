namespace GameLibrary.Models.Actions;

public partial class ActorDescriptionAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ActorDescription
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Actor Description";
}
