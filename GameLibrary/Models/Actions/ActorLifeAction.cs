namespace GameLibrary.Models.Actions;

public partial class ActorLifeAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public int LifeValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Life: {LifeValue:+#;-#;0}";
}
