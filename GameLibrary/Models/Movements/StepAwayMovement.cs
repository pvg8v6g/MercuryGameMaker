namespace GameLibrary.Models.Movements;

/// <summary>
/// Moves the entity Steps walking steps backward, opposite to the direction it is currently facing.
/// </summary>
public partial class StepAwayMovement : StepCountMovement
{
    public override string ToString() => $"Step Backward: {Steps} {(Steps == 1 ? "step" : "steps")} (Opposite Facing Direction)";
}
