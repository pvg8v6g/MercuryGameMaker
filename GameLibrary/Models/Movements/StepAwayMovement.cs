namespace GameLibrary.Models.Movements;

/// <summary>
/// Moves the entity one step backward, opposite to the direction it is currently facing.
/// </summary>
public partial class StepAwayMovement : Movement
{
    public override string ToString() => "1 Step Backward (Opposite Facing Direction)";
}
