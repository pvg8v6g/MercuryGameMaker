using MercuryLibrary.Extensions;

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
        var result = $"Entity State: {(Add ? "Add" : "Remove")}";
        if (!StateName.IsNullOrEmpty()) result += $"\n    State: {StateName}";
        return result;
    }
}
