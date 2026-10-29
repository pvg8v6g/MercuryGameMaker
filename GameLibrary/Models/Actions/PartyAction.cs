using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class PartyAction : PageAction
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

    public bool Add
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Initialize
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = $"Change Party: {(Add ? "Add" : "Remove")}";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        return result;
    }
}
