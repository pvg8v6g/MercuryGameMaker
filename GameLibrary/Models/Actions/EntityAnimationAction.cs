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

    public override string ToString() => "Show Animation";
}
