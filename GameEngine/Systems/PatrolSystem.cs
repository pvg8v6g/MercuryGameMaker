using GameEngine.Models;
using GameEngine.Navigation;
using GameEngine.Pages;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Systems;

/// <summary>
/// Runs every entity whose page has a patrol module. Patrol: walk the path point to point (or wander the zone, or stand
/// guard), stopping and looking around where the points say. Spotted: it sees the player, plays its balloon and stands
/// a moment. Chase: it goes after the player, finding its way around walls and anyone in the way. Walking into the
/// player runs the page's event touch. It gives up when the player is out of sight too long, it can't get any closer or
/// the chase runs too long. Return: back to the nearest point of its patrol, then it patrols again.
/// Patrols hold while a page the player started is running, and a movement route the page set takes over.
/// </summary>
public class PatrolSystem(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    HitboxSystem hitboxSystem,
    MovementSystem movementSystem,
    NavigationService navigationService,
    PathWalker pathWalker,
    BalloonSystem balloonSystem) : IEntitySystem
{
    #region Constants

    // updates between looks for the player
    private const int SightEvery = 6;

    // a looked along line is checked this many times a tile
    private const int SightSamplesPerTile = 4;

    // a spot in a zone is stood on for a random stop between these, in milliseconds
    private const int ZonePauseMin = 1000;
    private const int ZonePauseMax = 3000;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Properties

    private double Tile => gameDataService.GridSize * gameDataService.ScaleFactor;

    #endregion

    #region Update

    public void Update(MapEntity entity)
    {
        if (entity.GetModule<PatrolModule>() is not { } patrol || entity.GetModule<PositionModule>() is not { } position) return;
        if (gameStateService.IsPlayerBusy || entity.State<RouteState>().Route is not null) return;

        var state = entity.State<PatrolState>();
        if (!state.Started) Begin(entity, patrol, state);

        switch (state.Mode)
        {
            case PatrolMode.Patrol:
                Patrol(entity, position, patrol, state);
                break;
            case PatrolMode.Spotted:
                Spotted(entity, position, patrol, state);
                break;
            case PatrolMode.Chase:
                Chase(entity, position, patrol, state);
                break;
            case PatrolMode.Return:
                Return(entity, position, patrol, state);
                break;
        }
    }

    #endregion

    #region Modes

    private void Patrol(MapEntity entity, PositionModule position, PatrolModule patrol, PatrolState state)
    {
        SetSpeed(entity, patrol.PatrolSpeed);
        if (Spots(entity, position, patrol, state)) return;

        if (state.PauseUpdates > 0)
        {
            LookAround(entity, position, state);
            state.PauseUpdates--;
            return;
        }

        if (Goal(patrol, state) is not { } goal) return;

        switch (pathWalker.Follow(entity, position, state.Walk, goal))
        {
            case WalkResult.Done:
                Arrive(patrol, state);
                break;

            // a point it can't get to is skipped
            case WalkResult.Stuck:
                Next(patrol, state);
                break;
        }
    }

    // faces the player and stands for the spot pause with its balloon up, then the chase starts
    private void Spotted(MapEntity entity, PositionModule position, PatrolModule patrol, PatrolState state)
    {
        if (gameStateService.Player is { } player) Face(entity, position, player);
        if (--state.SpotUpdates > 0) return;

        state.Mode = PatrolMode.Chase;
        state.ChaseUpdates = 0;
        state.UnseenUpdates = 0;
        state.Walk.Reset();
    }

    private void Chase(MapEntity entity, PositionModule position, PatrolModule patrol, PatrolState state)
    {
        SetSpeed(entity, patrol.ChaseSpeed);
        if (gameStateService.Player is not { } player || player.GetModule<PositionModule>() is not { } playerPosition || hitboxSystem.Main(player) is not { } feet)
        {
            GiveUp(entity, patrol, state);
            return;
        }

        var limit = gameDataService.Updates(patrol.ChaseTimeLimit);
        state.UnseenUpdates = Sees(entity, position, patrol) ? 0 : state.UnseenUpdates + 1;
        if ((limit > 0 && ++state.ChaseUpdates >= limit) || state.UnseenUpdates >= gameDataService.Updates(patrol.LoseSightTime))
        {
            GiveUp(entity, patrol, state);
            return;
        }

        var goal = new NextToGoal([feet], HitboxSystem.Level(playerPosition), navigationService.CellPixels);
        switch (pathWalker.Follow(entity, position, state.Walk, goal))
        {
            // next to the player: walking into them is what runs the event touch page
            case WalkResult.Done:
                movementSystem.Move(entity, Facing(position, player));
                break;

            case WalkResult.Stuck:
                GiveUp(entity, patrol, state);
                break;
        }
    }

    private void Return(MapEntity entity, PositionModule position, PatrolModule patrol, PatrolState state)
    {
        SetSpeed(entity, patrol.PatrolSpeed);
        if (Spots(entity, position, patrol, state)) return;

        if (Goal(patrol, state) is not { } goal)
        {
            state.Mode = PatrolMode.Patrol;
            return;
        }

        var result = pathWalker.Follow(entity, position, state.Walk, goal);
        if (result is not (WalkResult.Done or WalkResult.Stuck)) return;

        state.Mode = PatrolMode.Patrol;
        if (result == WalkResult.Done) Arrive(patrol, state);
    }

    #endregion

    #region Patrol

    private void Begin(MapEntity entity, PatrolModule patrol, PatrolState state)
    {
        state.Started = true;
        state.Mode = PatrolMode.Patrol;
        state.Point = Math.Max(0, patrol.StartPoint - 1);
        if (hitboxSystem.Main(entity) is { } feet) state.Home = (feet.X, feet.Y, feet.Width, feet.Height);
        if (Path(patrol) is not { Points.Count: > 0 } path) return;

        state.Point = Math.Min(state.Point, path.Points.Count - 1);

        // back on a map where it was left mid patrol: it carries on from the point nearest to where it stands
        if (Returned(entity)) state.Point = NearestPoint(entity, path) ?? state.Point;
    }

    // its place was remembered when the player left the map, so it's not standing where it was placed
    private bool Returned(MapEntity entity)
    {
        return entity.ActivePage is { RemembersPosition: true }
               && gameStateService.ActiveMap is { } map
               && gameStateService.EntityPlaces.ContainsKey((map.Guid, entity.Entity.Guid));
    }

    // the path point closest to its feet
    private int? NearestPoint(MapEntity entity, PatrolPath path)
    {
        if (hitboxSystem.Main(entity) is not { } feet) return null;

        var x = feet.X + (feet.Width / 2.0);
        var y = feet.Y + (feet.Height / 2.0);
        return path.Points
            .Select((point, index) => (Index: index, Distance: Math.Abs(((point.X + 0.5) * Tile) - x) + Math.Abs(((point.Y + 0.5) * Tile) - y)))
            .MinBy(point => point.Distance).Index;
    }

    // the path point it's heading for, a spot in its zone, or where it stands guard
    private NavGoal? Goal(PatrolModule patrol, PatrolState state)
    {
        if (Path(patrol) is { Points.Count: > 0 } path)
        {
            var point = path.Points[Math.Clamp(state.Point, 0, path.Points.Count - 1)];
            return new SpotGoal(point.X * Tile, point.Y * Tile, Tile, Tile);
        }

        if (Zone(patrol) is { } zone)
        {
            state.Spot ??= RandomSpot(zone);
            return new SpotGoal(state.Spot.Value.X, state.Spot.Value.Y, state.Spot.Value.Width, state.Spot.Value.Height);
        }

        return state.Home is { } home ? new SpotGoal(home.X, home.Y, home.Width, home.Height) : null;
    }

    // stands at the point for its pause, looking around, then heads for the next one
    private void Arrive(PatrolModule patrol, PatrolState state)
    {
        state.Walk.Reset();
        if (Path(patrol) is { Points.Count: > 0 } path)
        {
            var point = path.Points[Math.Clamp(state.Point, 0, path.Points.Count - 1)];
            StartPause(state, gameDataService.Updates(point.PauseTime), point.Looks());
            Next(patrol, state);
            return;
        }

        if (Zone(patrol) is not null)
        {
            StartPause(state, gameDataService.Updates(_random.Next(ZonePauseMin, ZonePauseMax)), []);
            state.Spot = null;
        }
    }

    private void StartPause(PatrolState state, int updates, Direction[] looks)
    {
        state.PauseUpdates = updates;
        state.PauseTotal = updates;
        state.Looks = looks;
    }

    // the next point by the path's style
    private void Next(PatrolModule patrol, PatrolState state)
    {
        state.Walk.Reset();
        if (Path(patrol) is not { Points.Count: > 0 } path)
        {
            state.Spot = null;
            return;
        }

        var count = path.Points.Count;
        if (count < 2) return;

        switch (path.Style)
        {
            case PatrolStyle.Loop:
                state.Point = (state.Point + 1) % count;
                break;

            case PatrolStyle.BackAndForth:
                if (state.Forward && state.Point >= count - 1) state.Forward = false;
                else if (!state.Forward && state.Point <= 0) state.Forward = true;
                state.Point += state.Forward ? 1 : -1;
                break;

            case PatrolStyle.Wander:
                state.Point = (state.Point + 1 + _random.Next(count - 1)) % count;
                break;
        }
    }

    // the looks are spread evenly over the stop
    private static void LookAround(MapEntity entity, PositionModule position, PatrolState state)
    {
        if (state.Looks.Length == 0 || state.PauseTotal <= 0 || entity.GetModule<MovementModule>() is { FixDirection: true }) return;

        var done = state.PauseTotal - state.PauseUpdates;
        var index = Math.Min(state.Looks.Length - 1, done * state.Looks.Length / state.PauseTotal);
        position.Direction = state.Looks[index];
    }

    // walks back to the point nearest to it (or its zone or guard spot)
    private void GiveUp(MapEntity entity, PatrolModule patrol, PatrolState state)
    {
        state.Mode = PatrolMode.Return;
        state.Walk.Reset();
        state.PauseUpdates = 0;
        if (Path(patrol) is { Points.Count: > 0 } path && NearestPoint(entity, path) is { } nearest) state.Point = nearest;
    }

    #endregion

    #region Sight

    // looks for the player now and then, seeing them starts the spot
    private bool Spots(MapEntity entity, PositionModule position, PatrolModule patrol, PatrolState state)
    {
        if (!patrol.Chases || ++state.SightTimer < SightEvery) return false;

        state.SightTimer = 0;
        if (!Sees(entity, position, patrol)) return false;

        state.Mode = PatrolMode.Spotted;
        state.SpotUpdates = Math.Max(1, gameDataService.Updates(patrol.SpotPause));
        state.PauseUpdates = 0;
        state.Walk.Reset();
        balloonSystem.Show(entity, patrol.SpotBalloonId);
        return true;
    }

    // the player is on its level, within range, in front of it (unless it sees all around) and no solid wall or prop is
    // in the way. Collisions that only block some directions (ledges) don't block the view
    private bool Sees(MapEntity entity, PositionModule position, PatrolModule patrol)
    {
        if (gameStateService.Player is not { } player || player.GetModule<PositionModule>() is not { } playerPosition) return false;
        if (HitboxSystem.Level(position) != HitboxSystem.Level(playerPosition)) return false;
        if (hitboxSystem.Main(entity) is not { } eyes || hitboxSystem.Main(player) is not { } target) return false;

        var fromX = eyes.X + (eyes.Width / 2.0);
        var fromY = eyes.Y + (eyes.Height / 2.0);
        var dx = target.X + (target.Width / 2.0) - fromX;
        var dy = target.Y + (target.Height / 2.0) - fromY;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance > patrol.SightRange * Tile) return false;
        if (distance < 0.001) return true;

        // in front: within 60 degrees either side of the way it faces
        if (!patrol.SeesAllAround)
        {
            var (faceX, faceY) = position.Direction switch
            {
                Direction.Up => (0.0, -1.0),
                Direction.Down => (0.0, 1.0),
                Direction.Left => (-1.0, 0.0),
                _ => (1.0, 0.0)
            };
            if (((dx * faceX) + (dy * faceY)) / distance < 0.5) return false;
        }

        var level = HitboxSystem.Level(position);
        var walls = gameStateService.MapAreas
            .Where(x => x.AreaType == MapAreaType.Collision && x.Level == level && x is { BlocksUp: true, BlocksDown: true, BlocksLeft: true, BlocksRight: true })
            .Where(x => !Inside(x, fromX, fromY) && !Inside(x, fromX + dx, fromY + dy))
            .ToArray();
        var samples = (int) Math.Ceiling(distance / Tile * SightSamplesPerTile);
        for (var i = 1; i < samples; i++)
        {
            var t = (double) i / samples;
            if (walls.Any(wall => Inside(wall, fromX + (dx * t), fromY + (dy * t)))) return false;
        }

        return true;
    }

    private bool Inside(MapArea area, double x, double y)
    {
        var scale = gameDataService.ScaleFactor;
        return x >= area.OffsetX * scale && x < (area.OffsetX + area.Width) * scale && y >= area.OffsetY * scale && y < (area.OffsetY + area.Height) * scale;
    }

    #endregion

    #region Private Methods

    private PatrolPath? Path(PatrolModule patrol)
    {
        if (string.IsNullOrEmpty(patrol.PathName) || gameStateService.ActiveMap is not { } map) return null;
        return map.PatrolPaths.FirstOrDefault(x => string.Equals(x.Name, patrol.PathName, StringComparison.OrdinalIgnoreCase));
    }

    private MapArea? Zone(PatrolModule patrol)
    {
        if (string.IsNullOrEmpty(patrol.ZoneName)) return null;
        return gameStateService.MapAreas.FirstOrDefault(x => string.Equals(x.Name, patrol.ZoneName, StringComparison.OrdinalIgnoreCase));
    }

    // a tile inside the zone, in scaled map pixels
    private (double X, double Y, double Width, double Height) RandomSpot(MapArea zone)
    {
        var scale = gameDataService.ScaleFactor;
        var columns = Math.Max(1, (int) (zone.Width * scale / Tile));
        var rows = Math.Max(1, (int) (zone.Height * scale / Tile));
        var x = (zone.OffsetX * scale) + (_random.Next(columns) * Tile);
        var y = (zone.OffsetY * scale) + (_random.Next(rows) * Tile);
        return (x, y, Tile, Tile);
    }

    private static void SetSpeed(MapEntity entity, int speed)
    {
        if (entity.GetModule<MovementModule>() is { } movement && speed > 0) movement.Speed = speed;
    }

    private static void Face(MapEntity entity, PositionModule position, MapEntity other)
    {
        if (entity.GetModule<MovementModule>() is { FixDirection: true }) return;
        position.Direction = Facing(position, other);
    }

    private static Direction Facing(PositionModule position, MapEntity other)
    {
        if (other.GetModule<PositionModule>() is not { } to) return position.Direction;

        var dx = to.X - position.X;
        var dy = to.Y - position.Y;
        return Math.Abs(dx) > Math.Abs(dy)
            ? dx > 0 ? Direction.Right : Direction.Left
            : dy > 0 ? Direction.Down : Direction.Up;
    }

    #endregion
}
