using GameEngine.Navigation;
using GameLibrary.Enumerations;

namespace GameEngine.Models;

/// <summary>
/// Where a patroller is in its patrol: walking it, just spotted the player, chasing, or walking back after giving up.
/// </summary>
public class PatrolState
{
    public bool Started { get; set; }

    public PatrolMode Mode { get; set; }

    // the path point it's heading for (0 is the first) and which way it's going on a back and forth path
    public int Point { get; set; }

    public bool Forward { get; set; } = true;

    // standing on a point: updates left, how long the stop is and the ways to look while standing
    public int PauseUpdates { get; set; }

    public int PauseTotal { get; set; }

    public Direction[] Looks { get; set; } = [];

    // a spot picked in its zone, or where it stood guard (the feet, in scaled map pixels)
    public (double X, double Y, double Width, double Height)? Spot { get; set; }

    public (double X, double Y, double Width, double Height)? Home { get; set; }

    public int SpotUpdates { get; set; }

    public int ChaseUpdates { get; set; }

    public int UnseenUpdates { get; set; }

    // looking for the player isn't needed every update
    public int SightTimer { get; set; }

    public WalkState Walk { get; } = new();
}

public enum PatrolMode
{
    Patrol,
    Spotted,
    Chase,
    Return
}
