using GameEngine.Models;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;

namespace GameEngine.Navigation;

/// <summary>
/// Walks an entity a character step at a time: a set distance one way, or along a path to a goal, searching again every
/// path cell so it follows whoever it's after. Every step is the normal walking step with its collision check, so a
/// walker never goes through anyone. Blocked, it waits a moment (plus a random bit, so two walkers don't keep
/// bumping into each other) and after a few times in a row it shuffles one cell aside before searching again.
/// Routes and patrols both walk with this.
/// </summary>
public class PathWalker(MovementSystem movementSystem, NavigationService navigationService)
{
    #region Constants

    // a blocked walker waits this many updates (plus a random few) before trying again
    private const int BlockedWait = 10;

    // after being blocked this many times in a row a walker shuffles one cell aside before searching again
    private const int ShuffleAfter = 3;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Methods

    // a distance one way, keep facing walks without turning (a step backward)
    public WalkResult Walk(MapEntity entity, PositionModule position, WalkState state, Direction way, double pixels, bool keepFacing = false)
    {
        if (!state.Started) Begin(state, position, way, pixels, keepFacing);
        return Step(entity, position, state);
    }

    // along a path to the goal. Stuck: the walker can't get any closer than where it is
    public WalkResult Follow(MapEntity entity, PositionModule position, WalkState state, NavGoal goal)
    {
        if (state.BlockedUpdates > 0 && --state.BlockedUpdates > 0) return WalkResult.Busy;
        if (entity.State<MovementState>().IsMoving) return WalkResult.Busy;

        // mid cell: keep walking it
        if (state.Started)
        {
            var walked = Step(entity, position, state);
            if (walked == WalkResult.Busy) return walked;
            if (walked == WalkResult.Blocked) return Blocked(entity, position, state);

            state.Started = false;
            state.TimesBlocked = 0;
        }

        if (navigationService.FindPath(entity, goal) is not { } path) return WalkResult.Busy;
        if (path.Cells.Count == 0) return path.Reached ? WalkResult.Done : WalkResult.Stuck;

        Begin(state, position, path.Cells[0], navigationService.CellPixels, false);
        return Step(entity, position, state) == WalkResult.Blocked ? Blocked(entity, position, state) : WalkResult.Busy;
    }

    #endregion

    #region Private Methods

    private static void Begin(WalkState state, PositionModule position, Direction way, double pixels, bool keepFacing)
    {
        var (unitX, unitY) = Unit(way);
        state.Started = true;
        state.Way = way;
        state.KeepFacing = keepFacing;
        state.TargetX = position.X + (unitX * pixels);
        state.TargetY = position.Y + (unitY * pixels);
    }

    // takes character steps until the target is reached
    private WalkResult Step(MapEntity entity, PositionModule position, WalkState state)
    {
        var movement = entity.State<MovementState>();
        if (movement.IsMoving) return WalkResult.Busy;

        if (Math.Abs(state.TargetX - position.X) + Math.Abs(state.TargetY - position.Y) < 0.5)
        {
            position.X = state.TargetX;
            position.Y = state.TargetY;
            state.Started = false;
            return WalkResult.Done;
        }

        var movementModule = entity.GetModule<MovementModule>();
        var fixedBefore = movementModule?.FixDirection ?? false;
        if (state.KeepFacing && movementModule is not null) movementModule.FixDirection = true;
        // never past the target: a speed that doesn't fit the walk evenly takes a shorter last step
        var remaining = Math.Abs(state.TargetX - position.X) + Math.Abs(state.TargetY - position.Y);
        movementSystem.Move(entity, state.Way, remaining);
        if (movementModule is not null) movementModule.FixDirection = fixedBefore;

        return movement.MoveInterrupted && !movement.IsMoving ? WalkResult.Blocked : WalkResult.Busy;
    }

    private WalkResult Blocked(MapEntity entity, PositionModule position, WalkState state)
    {
        state.Started = false;
        state.BlockedUpdates = BlockedWait + _random.Next(BlockedWait);
        if (++state.TimesBlocked < ShuffleAfter) return WalkResult.Blocked;

        state.TimesBlocked = 0;
        if (navigationService.RandomOpenDirection(entity) is not { } aside) return WalkResult.Blocked;

        Begin(state, position, aside, navigationService.CellPixels, false);
        state.BlockedUpdates = 0;
        return WalkResult.Blocked;
    }

    private static (double X, double Y) Unit(Direction way) => way switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };

    #endregion
}

public enum WalkResult
{
    // got there
    Done,

    // still walking (or waiting to search)
    Busy,

    // something is in the way of the next step
    Blocked,

    // following a path: nowhere closer to the goal can be got to
    Stuck
}

/// <summary>
/// A walk in progress: where it's heading in scaled map pixels and how often it's been blocked.
/// </summary>
public class WalkState
{
    public bool Started { get; set; }

    public double TargetX { get; set; }

    public double TargetY { get; set; }

    public Direction Way { get; set; }

    public bool KeepFacing { get; set; }

    public int BlockedUpdates { get; set; }

    public int TimesBlocked { get; set; }

    public void Reset()
    {
        Started = false;
        BlockedUpdates = 0;
        TimesBlocked = 0;
    }
}
