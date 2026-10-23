using GameLibrary.Enumerations;

namespace GameLibrary.Models.Actions;

public partial class EntityTransferAction : PageAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? MapId
    {
        get;
        set => SetField(ref field, value);
    }

    public int X
    {
        get;
        set => SetField(ref field, value);
    }

    public int Y
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    public override string ToString() => "Transfer Entity";
}
