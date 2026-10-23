namespace GameLibrary.Models.Actions;

public partial class ActorExperienceAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public int Experience
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Experience: {Experience:+#;-#;0}";
}
