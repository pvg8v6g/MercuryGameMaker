using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class MoveMovement : Movement
{
    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    // a random way each time the step runs, instead of the direction
    public bool Random
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => Random ? "Move Once: Random" : $"Move Once: {Direction}";
}
