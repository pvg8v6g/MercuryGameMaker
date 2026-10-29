using GameLibrary.Enumerations;
using MercuryLibrary.Extensions;

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

    public string? MapName
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

    public override string ToString()
    {
        var result = "Transfer Entity";
        if (!MapName.IsNullOrEmpty()) result += $"\n    Map: {MapName}";
        result += $"\n    To: {X}, {Y}\n    Facing: {Direction}";
        return result;
    }
}
