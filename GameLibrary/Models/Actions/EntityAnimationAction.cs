using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class EntityAnimationAction : PageAction, IEntityTargetAction
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

    public Guid? AnimationId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? AnimationName
    {
        get;
        set => SetField(ref field, value);
    }

    // the page waits until the animation has played through
    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() =>
        (AnimationName.IsNullOrEmpty() ? "Show Animation" : $"Show Animation: {AnimationName}") + $"\n    Entity: {this.TargetText()}";
}
