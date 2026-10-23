using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class MoveMovement : Movement
{
    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    public override string ToString() => $"Move Once: {Direction}";
}
