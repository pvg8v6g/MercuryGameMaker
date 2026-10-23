namespace GameLibrary.Models.Movements;

/// <summary>
/// Moves the entity one step in the direction it is currently facing.
/// </summary>
public partial class StepForwardMovement : Movement
{
    public override string ToString() => "1 Step Forward (Facing Direction)";
}
