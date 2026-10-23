namespace GameLibrary.Models.Actions;

public partial class ActorManaAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Mana: {ManaValue:+#;-#;0}";
}
