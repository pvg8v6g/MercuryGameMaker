namespace GameLibrary.Models.Conditions;

public partial class ActorCondition : Condition
{
    public Guid? OwnActor
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? DoNotOwnActor
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() =>
        OwnActor != null ? "Actor In Party" :
        DoNotOwnActor != null ? "Actor Not In Party" :
        "Actor Condition";
}
