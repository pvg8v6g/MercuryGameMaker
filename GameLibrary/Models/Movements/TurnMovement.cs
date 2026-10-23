using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class TurnMovement : Movement
{
    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    public override string ToString() => $"Turn: {Direction}";
}
