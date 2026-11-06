namespace GameEngine.Models;

/// <summary>
/// The step an entity is taking right now. The movement module holds the settings (speed, dashing),
/// this holds how far is left to walk.
/// </summary>
public class MovementState
{
    #region Properties

    public bool MoveInterrupted { get; set; }

    public bool WalkAroundInterrupted { get; set; }

    // distance left to walk this step
    public double DistanceX { get; set; }

    public double DistanceY { get; set; }

    // distance walked each update
    public double IncrementX { get; set; }

    public double IncrementY { get; set; }

    public bool IsMoving => DistanceX > 0 || DistanceY > 0;

    // walked this update, a step can finish in the same update it moves so IsMoving alone would look idle
    public bool Stepped { get; set; }

    #endregion
}
