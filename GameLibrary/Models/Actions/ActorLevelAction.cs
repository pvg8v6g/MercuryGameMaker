namespace GameLibrary.Models.Actions;

public partial class ActorLevelAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public int Level
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Level: {Level:+#;-#;0}";
}
