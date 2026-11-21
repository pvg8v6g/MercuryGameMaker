namespace GameLibrary.Models.Movements;

/// <summary>
/// Moves the entity Steps walking steps in the direction it is currently facing.
/// </summary>
public partial class StepForwardMovement : StepCountMovement
{
    public override string ToString() => $"Step Forward: {Steps} {(Steps == 1 ? "step" : "steps")} (Facing Direction)";
}
