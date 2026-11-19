using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class EntityStateAction : PageAction, IEntityTargetAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    // the player (the party leader) instead of an entity
    public bool TargetsPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    // the picked entity as "001: name" for the action list, none for the page's own entity
    public string? TargetName
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
        var result = $"Entity State: {(Add ? "Add" : "Remove")}\n    Entity: {this.TargetText()}";
        if (!StateName.IsNullOrEmpty()) result += $"\n    State: {StateName}";
        return result;
    }
}
