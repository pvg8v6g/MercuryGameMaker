namespace GameLibrary.Models.Actions;

public partial class EntityStateAction : PageAction
{
    public Guid? EntityId
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

    public override string ToString() => $"Entity State: {(Add ? "Add" : "Remove")}";
}
