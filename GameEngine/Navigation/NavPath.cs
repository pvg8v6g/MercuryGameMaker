using GameLibrary.Enumerations;

namespace GameEngine.Navigation;

/// <summary>
/// The way to a goal, one direction per path cell. Reached is false when the goal can't be got to: the path then
/// goes to the reachable spot closest to it (or is empty when nothing is closer than where the walker stands).
/// </summary>
public class NavPath(List<Direction> cells, bool reached)
{
    public static NavPath None { get; } = new([], false);

    public List<Direction> Cells => cells;

    public bool Reached => reached;
}
