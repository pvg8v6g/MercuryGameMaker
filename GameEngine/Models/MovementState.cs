namespace GameEngine.Models;

/// <summary>
/// The step an entity is taking right now. The movement module holds the settings (speed, dashing, stepping),
/// this holds the distance left to walk and the timers that animate it.
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

    public int SteppingTimer { get; set; }

    public int NotMovingTimer { get; set; }

    public bool IsMoving => DistanceX > 0 || DistanceY > 0;

    #endregion
}
