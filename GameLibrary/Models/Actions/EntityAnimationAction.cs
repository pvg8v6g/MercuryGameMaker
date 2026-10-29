using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class EntityAnimationAction : PageAction
{
    public Guid? EntityId
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

    public override string ToString() =>
        AnimationName.IsNullOrEmpty() ? "Show Animation" : $"Show Animation: {AnimationName}";
}
