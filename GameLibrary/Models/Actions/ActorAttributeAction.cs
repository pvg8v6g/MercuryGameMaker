namespace GameLibrary.Models.Actions;

public partial class ActorAttributeAction : PageAction
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

    public Guid? AttributeId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? AttributeName
    {
        get;
        set => SetField(ref field, value);
    }

    public int Amount
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Actor Attribute: {AttributeName} {Amount:+#;-#;0}";
}
