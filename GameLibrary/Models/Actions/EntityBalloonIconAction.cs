namespace GameLibrary.Models.Actions;

public partial class EntityBalloonIconAction : PageAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public int BalloonIconIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Show Balloon Icon";
}
