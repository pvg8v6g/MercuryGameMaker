using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class TurnMovement : Movement
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

    public override string ToString() => Random ? "Turn: Random" : $"Turn: {Direction}";
}
