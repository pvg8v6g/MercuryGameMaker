using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Navigation;

/// <summary>
/// Finds the way for an entity to get somewhere: around walls, collision areas (by level and the directions they block),
/// prop footprints and entities standing still. The search (A*) moves in cells of a few character steps and tests every
/// step with the walker's own hitboxes, the same test walking does, so a path is always one the entity can walk.
/// Level changes on the way (stairs) are followed, so a path can go up onto a platform or over a bridge.
/// Entities that are moving are left out, they won't be there by the time the walker is: followers check every step
/// and search again when someone is in the way. Searches are limited per update so many walkers never slow the game.
/// </summary>
public class NavigationService(IGameDataService gameDataService, IGameStateService gameStateService, HitboxSystem hitboxSystem)
{
    #region Constants

    // character steps in a path cell, 3 steps of 4 pixels is a quarter tile
    public const int StepsPerCell = 3;

    // cells looked at before a search settles for the closest spot it found
    private const int MaxSearchedCells = 2500;

    // searches every walker together can start in one update, the rest wait an update
    private const int SearchesPerUpdate = 4;

    private static readonly Direction[] Directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    #endregion

    #region Fields

    private readonly Random _random = new();
    private int _searchesLeft = SearchesPerUpdate;

    #endregion

    #region Properties

    // a character step and a path cell in scaled map pixels
    public double StepPixels => gameDataService.StepSize * gameDataService.ScaleFactor;

    public double CellPixels => StepPixels * StepsPerCell;

    #endregion

    #region Methods

    // called once an update, every update gets its own searches
    public void NewUpdate() => _searchesLeft = SearchesPerUpdate;

    // the path to the goal, null when this update's searches are used up (ask again next update)
    public NavPath? FindPath(MapEntity walker, NavGoal goal)
    {
        if (_searchesLeft <= 0) return null;
        _searchesLeft--;

        if (Walker(walker) is not { } search) return NavPath.None;

        var start = new Cell(0, 0, HitboxSystem.Level(search.Position));
        var open = new PriorityQueue<Cell, double>();
        var walked = new Dictionary<Cell, double> { [start] = 0 };
        var cameFrom = new Dictionary<Cell, (Cell From, Direction Way)>();
        var closed = new HashSet<Cell>();
        var best = start;
        var bestRemaining = goal.Remaining(Feet(search, start));
        open.Enqueue(start, bestRemaining / CellPixels);

        while (open.TryDequeue(out var cell, out _) && closed.Count < MaxSearchedCells)
        {
            if (!closed.Add(cell)) continue;

            var feet = Feet(search, cell);
            if (goal.Reached(feet, cell.Level)) return new NavPath(PathTo(cell, cameFrom), true);

            var remaining = goal.Remaining(feet);
            if (remaining < bestRemaining)
            {
                best = cell;
                bestRemaining = remaining;
            }

            foreach (var way in Directions)
            {
                if (Step(search, cell, way) is not { } next || closed.Contains(next)) continue;

                var cost = walked[cell] + 1;
                if (walked.TryGetValue(next, out var known) && known <= cost) continue;

                walked[next] = cost;
                cameFrom[next] = (cell, way);
                open.Enqueue(next, cost + (goal.Remaining(Feet(search, next)) / CellPixels));
            }
        }

        return new NavPath(PathTo(best, cameFrom), false);
    }

    // a direction the walker can take one path cell in right now, picked at random. A walker that keeps getting
    // blocked (two walkers face to face in a hall) shuffles aside with it before searching again
    public Direction? RandomOpenDirection(MapEntity walker)
    {
        if (Walker(walker, includeMoving: true) is not { } search) return null;

        var start = new Cell(0, 0, HitboxSystem.Level(search.Position));
        return Directions.OrderBy(_ => _random.Next()).Cast<Direction?>().FirstOrDefault(way => Step(search, start, way!.Value) is not null);
    }

    #endregion

    #region Private Methods

    // what a search needs about the walker and the map, made once per search
    private Search? Walker(MapEntity walker, bool includeMoving = false)
    {
        if (walker.GetModule<PositionModule>() is not { } position || walker.GetModule<EntityHitboxModule>() is not { } hitboxModule) return null;
        if (!HitboxSystem.Areas(hitboxModule, Direction.None).Any() || gameStateService.ActiveMap is not { } map) return null;

        var tile = gameDataService.GridSize * gameDataService.ScaleFactor;
        return new Search(position, hitboxModule, map.Width * tile, map.Height * tile, Obstacles(walker, includeMoving));
    }

    // the entities that block the walker, by level: everyone that collides and (unless asked) isn't on the move
    private Dictionary<int, MapHitbox[]> Obstacles(MapEntity walker, bool includeMoving)
    {
        return gameStateService.MapEntities
            .Where(x => x != walker && !x.IsErased && x.GetModule<EntityHitboxModule>() is { Collide: true })
            .Where(x => includeMoving || !x.State<MovementState>().IsMoving)
            .Where(x => x.GetModule<PositionModule>() is not null)
            .GroupBy(x => HitboxSystem.Level(x.GetModule<PositionModule>()!))
            .ToDictionary(x => x.Key, x => x.SelectMany(hitboxSystem.PlacedOf).ToArray());
    }

    // walks one path cell a character step at a time, the way walking checks each step: the map edge, collision areas on
    // the level that block the direction and entities in the way. Stairs on the way change the level. Null when blocked
    private Cell? Step(Search search, Cell cell, Direction way)
    {
        var (unitX, unitY) = Unit(way);
        var x = X(search, cell);
        var y = Y(search, cell);
        var level = cell.Level;
        var current = hitboxSystem.PlaceAt(search.HitboxModule, x, y, way);
        for (var step = 0; step < StepsPerCell; step++)
        {
            x += unitX * StepPixels;
            y += unitY * StepPixels;
            var projected = hitboxSystem.PlaceAt(search.HitboxModule, x, y, way);
            if (projected.Any(box => box.X < 0 || box.Y < 0 || box.X + box.Width > search.MapWidth || box.Y + box.Height > search.MapHeight)) return null;

            if (search.HitboxModule.Collide)
            {
                if (hitboxSystem.HitsCollisionArea(current, projected, way, level)) return null;
                if (search.Obstacles.TryGetValue(level, out var others) && others.Any(other => projected.Any(other.Collide) && !current.Any(other.Collide))) return null;
            }

            current = projected;
            level = hitboxSystem.LevelChangeAt(FeetAt(search, x, y)) ?? level;
        }

        return new Cell(cell.I + (int) unitX, cell.J + (int) unitY, level);
    }

    private MapHitbox Feet(Search search, Cell cell) => FeetAt(search, X(search, cell), Y(search, cell));

    // the walker's main hitbox, the first one used in every direction
    private MapHitbox FeetAt(Search search, double x, double y) => hitboxSystem.PlaceAt(search.HitboxModule, x, y, Direction.None)[0];

    private double X(Search search, Cell cell) => search.Position.X + (cell.I * CellPixels);

    private double Y(Search search, Cell cell) => search.Position.Y + (cell.J * CellPixels);

    private static (double X, double Y) Unit(Direction way) => way switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };

    private static List<Direction> PathTo(Cell cell, Dictionary<Cell, (Cell From, Direction Way)> cameFrom)
    {
        var path = new List<Direction>();
        while (cameFrom.TryGetValue(cell, out var previous))
        {
            path.Add(previous.Way);
            cell = previous.From;
        }

        path.Reverse();
        return path;
    }

    #endregion

    #region Types

    // a path cell counted from where the walker stands, and the level the walker is on there
    private readonly record struct Cell(int I, int J, int Level);

    private record Search(PositionModule Position, EntityHitboxModule HitboxModule, double MapWidth, double MapHeight, Dictionary<int, MapHitbox[]> Obstacles);

    #endregion
}
