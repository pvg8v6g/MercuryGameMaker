using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorStateAction : PageAction
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

    public Guid? StateId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? StateName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Add
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = $"Actor State: {(Add ? "Add" : "Remove")}";
        if (!ActorName.IsNullOrEmpty()) result += $"\n    Actor: {ActorName}";
        if (!StateName.IsNullOrEmpty()) result += $"\n    State: {StateName}";
        return result;
    }
}
