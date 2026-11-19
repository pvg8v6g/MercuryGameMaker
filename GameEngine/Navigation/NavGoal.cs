using GameEngine.Models;

namespace GameEngine.Navigation;

/// <summary>
/// Where a path is going. A goal says when the walker's feet have arrived and roughly how far they still are,
/// smaller is closer. A goal that can't be reached gets a path to the reachable spot that's closest.
/// </summary>
public abstract class NavGoal
{
    public abstract bool Reached(MapHitbox feet, int level);

    public abstract double Remaining(MapHitbox feet);

    protected static (double X, double Y) Center(MapHitbox box) => (box.X + (box.Width / 2.0), box.Y + (box.Height / 2.0));

    protected static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}

/// <summary>
/// Feet on a spot of the map (a tile, a patrol point), in scaled map pixels.
/// </summary>
public class SpotGoal(double x, double y, double width, double height) : NavGoal
{
    public override bool Reached(MapHitbox feet, int level) => feet.Collide(x, y, width, height);

    public override double Remaining(MapHitbox feet) => Distance(Center(feet), (x + (width / 2.0), y + (height / 2.0)));
}

/// <summary>
/// Next to another entity: the feet are within reach of one of its hitboxes, on its level.
/// The entity's hitboxes are taken when the search starts, a moving target is followed by searching again.
/// </summary>
public class NextToGoal(MapHitbox[] target, int level, double reach) : NavGoal
{
    private readonly (double X, double Y) _center = target.Length > 0 ? Center(target[0]) : (0, 0);

    public override bool Reached(MapHitbox feet, int walkerLevel) =>
        walkerLevel == level && target.Any(box => box.Collide(feet.X - reach, feet.Y - reach, feet.Width + (reach * 2), feet.Height + (reach * 2)));

    public override double Remaining(MapHitbox feet) => Distance(Center(feet), _center);
}

/// <summary>
/// At least a distance away from a point. Remaining is how much farther there is to go, so the search heads away and
/// prefers the reachable spot farthest off when it can't get the whole distance (no running into a corner).
/// </summary>
public class AwayGoal(double x, double y, double distance) : NavGoal
{
    public override bool Reached(MapHitbox feet, int level) => Distance(Center(feet), (x, y)) >= distance;

    public override double Remaining(MapHitbox feet) => Math.Max(0, distance - Distance(Center(feet), (x, y)));
}
