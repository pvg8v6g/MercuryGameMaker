namespace GameLibrary.Models.Actions;

public partial class ActorStateAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? StateId
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Add
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor State: {(Add ? "Add" : "Remove")}";
}
