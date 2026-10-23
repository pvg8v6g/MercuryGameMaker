namespace GameLibrary.Models.Actions;

public partial class ActorArteAction : PageAction
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

    public Guid? ArteId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ArteName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Add
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public override string ToString() => $"Actor Arte: {(Add ? "Add" : "Remove")} {ArteName}";
}
