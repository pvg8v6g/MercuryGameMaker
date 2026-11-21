using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class MoveMovement : StepCountMovement
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

    public override string ToString() => $"Move: {(Random ? "Random" : Direction.ToString())}, {Steps} {(Steps == 1 ? "step" : "steps")}";
}
