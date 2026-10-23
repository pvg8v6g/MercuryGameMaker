namespace GameLibrary.Models.Actions;

public partial class ActorNameAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ActorName
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Name: {ActorName}";
}
