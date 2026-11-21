using GameEngine.Models;
using GameEngine.Navigation;
using GameEngine.Pages;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Modules;
using GameLibrary.Models.Movements;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;

namespace GameEngine.Systems;

/// <summary>
/// Walks entities through their movement routes one step at a time. A move is a tile, waits are in milliseconds.
/// A blocked step waits until the way is clear, or is skipped when the route skips what it can't do.
/// Move toward and move away find their way with the navigation service and search again as they go.
/// Steps for systems that don't exist yet (artes, detonate) are skipped.
/// </summary>
public class MovementRouteSystem(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IAudioService audioService,
    NavigationService navigationService,
    PathWalker pathWalker,
    HitboxSystem hitboxSystem,
    BalloonSystem balloonSystem) : IEntitySystem
{
    #region Constants

    private static readonly Direction[] Directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Properties

    private double Tile => gameDataService.GridSize * gameDataService.ScaleFactor;

    // one walking step (the grid divided by Division), the unit a move step's Steps count is in
    private double StepPixels => gameDataService.StepSize * gameDataService.ScaleFactor;

    #endregion

    #region Methods

    // puts the entity on the route from its first step, any route it was on stops
    public void Start(MapEntity entity, EntityMovementAction route)
    {
        var state = entity.State<RouteState>();
        state.Route = route;
        state.Index = 0;
        state.StepStarted = false;
        state.Walk.Reset();
    }

    // whether the entity is still on the route, a repeating route never finishes
    public bool IsFollowing(MapEntity entity, EntityMovementAction route) => !entity.IsErased && entity.State<RouteState>().Route == route;

    public void Update(MapEntity entity)
    {
        UpdateAppearance(entity);

        var state = entity.State<RouteState>();
        if (state.Route is not { } route || entity.GetModule<PositionModule>() is not { } position) return;

        // instant steps run one after another, a step that takes time holds the route
        for (var count = 0; count < route.Route.Count + 1; count++)
        {
            if (state.Index >= route.Route.Count)
            {
                if (!route.Repeat || route.Route.Count == 0)
                {
                    state.Route = null;
                    return;
                }

                state.Index = 0;
            }

            var movement = route.Route[state.Index];
            var target = Target(entity, movement);
            var result = target?.GetModule<PositionModule>() is { } targetPosition ? Run(target, targetPosition, state, movement) : StepResult.Done;

            if (result == StepResult.Busy) return;
            if (result == StepResult.Blocked && !route.SkipIfInterrupted) return;

            state.Index++;
            state.StepStarted = false;
            state.Walk.Reset();
        }
    }

    #endregion

    #region Steps

    private StepResult Run(MapEntity entity, PositionModule position, RouteState state, Movement movement)
    {
        switch (movement)
        {
            case MoveMovement { Random: false } move:
                return Result(pathWalker.Walk(entity, position, state.Walk, move.Direction, move.Steps * StepPixels));

            // a random way is picked once for the step, and one that's blocked is just skipped
            case MoveMovement randomMove:
                if (!state.StepStarted)
                {
                    state.StepStarted = true;
                    state.Way = Directions[_random.Next(Directions.Length)];
                }

                var random = Result(pathWalker.Walk(entity, position, state.Walk, state.Way, randomMove.Steps * StepPixels));
                return random == StepResult.Blocked ? StepResult.Done : random;

            case StepForwardMovement forward:
                return Result(pathWalker.Walk(entity, position, state.Walk, position.Direction, forward.Steps * StepPixels));

            // backward keeps facing the same way
            case StepAwayMovement backward:
                return Result(pathWalker.Walk(entity, position, state.Walk, Opposite(position.Direction), backward.Steps * StepPixels, true));

            // someone that can't be got to keeps being tried, running away is done once it can't get any farther
            case MoveTowardMovement toward:
                return TowardGoal(entity, toward) is { } towardGoal ? Result(pathWalker.Follow(entity, position, state.Walk, towardGoal)) : StepResult.Done;

            case MoveAwayMovement away:
                if (AwayGoal(away) is not { } awayGoal) return StepResult.Done;
                var fled = pathWalker.Follow(entity, position, state.Walk, awayGoal);
                return fled == WalkResult.Stuck ? StepResult.Done : Result(fled);

            case WaitMovement wait:
                return Wait(state, wait.Wait);

            case TurnMovement turn:
                Turn(entity, position, turn.Random ? Directions[_random.Next(Directions.Length)] : turn.Direction);
                return StepResult.Done;

            case Turn180Movement:
                Turn(entity, position, Opposite(position.Direction));
                return StepResult.Done;

            case TurnTowardEntityMovement toward:
                if (Other(toward.TargetPlayer, toward.TargetEntityId) is { } towardOther) Turn(entity, position, Facing(entity, position, towardOther));
                return StepResult.Done;

            case TurnAwayEntityMovement away:
                if (Other(away.TargetPlayer, away.TargetEntityId) is { } awayOther) Turn(entity, position, Opposite(Facing(entity, position, awayOther)));
                return StepResult.Done;

            case SpeedMovement speed:
                if (entity.GetModule<MovementModule>() is { } speedModule) speedModule.Speed = speed.Speed;
                return StepResult.Done;

            case DirectionFixMovement fix:
                if (entity.GetModule<MovementModule>() is { } fixModule) fixModule.FixDirection = !fix.Disabled;
                return StepResult.Done;

            // through on walks through everything
            case ThroughMovement through:
                if (entity.GetModule<EntityHitboxModule>() is { } hitboxModule) hitboxModule.Collide = through.Disabled;
                return StepResult.Done;

            // the stepping animation keeps the feet going while standing
            case SteppingMovement stepping:
                if (entity.GetModule<MovementModule>() is { } steppingModule) steppingModule.AutoStepping = !stepping.Disabled;
                return StepResult.Done;

            case OpacityMovement opacity:
                FadeTo(entity, opacity.Opacity, opacity.TranslateTime);
                return StepResult.Done;

            case BlendModeMovement blend:
                entity.State<AppearanceState>().BlendMode = blend.BlendMode;
                return StepResult.Done;

            case CharacterImageMovement image:
                if (entity.GetModule<CharacterImageModule>() is { } imageModule)
                {
                    imageModule.ImageName = image.CharacterName;
                    imageModule.Index = image.CharacterIndex;
                    position.Direction = image.Direction;
                }

                return StepResult.Done;

            // waiting holds the route until the balloon has played through and gone
            case BalloonMovement balloon:
                if (!state.StepStarted)
                {
                    state.StepStarted = true;
                    balloonSystem.Show(entity, balloon.BalloonIconId);
                }

                return balloon.WaitForCompletion && balloonSystem.IsShowing(entity) ? StepResult.Busy : StepResult.Done;

            case FlowMovement flow:
                Flow(entity, flow);
                return StepResult.Done;

            case PlayMediaMovement media:
                if (!string.IsNullOrEmpty(media.MediaName)) audioService.PlaySfx(media.MediaName, media.Volume, media.Pitch);
                return StepResult.Done;

            default:
                return StepResult.Done;
        }
    }

    // the player, the picked entity, or the entity the route belongs to
    private MapEntity? Target(MapEntity entity, Movement movement)
    {
        if (movement.TargetsPlayer && movement.HasEntityTarget) return gameStateService.Player;
        if (movement.EntityId is { } guid && movement.HasEntityTarget) return gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Guid == guid);
        return entity;
    }

    // a stuck walker is blocked as far as the route goes: it keeps trying, or skips the step
    private static StepResult Result(WalkResult result) => result switch
    {
        WalkResult.Done => StepResult.Done,
        WalkResult.Busy => StepResult.Busy,
        _ => StepResult.Blocked
    };

    private StepResult Wait(RouteState state, int milliseconds)
    {
        if (!state.StepStarted)
        {
            state.StepStarted = true;
            state.WaitUpdates = gameDataService.Updates(milliseconds);
        }

        return state.WaitUpdates-- > 0 ? StepResult.Busy : StepResult.Done;
    }

    #endregion

    #region Goals

    // next to the player or an entity (by its id on the map), or feet on a tile of the map
    private NavGoal? TowardGoal(MapEntity walker, MoveTowardMovement toward)
    {
        if (toward.TargetType == MovementTarget.Location) return new SpotGoal(toward.TargetX * Tile, toward.TargetY * Tile, Tile, Tile);
        if (Other(toward.TargetType == MovementTarget.Player, toward.TargetEntityId) is not { } other || other == walker) return null;

        var boxes = HitboxesOf(other);
        if (boxes.Length == 0 || other.GetModule<PositionModule>() is not { } position) return null;
        return new NextToGoal(boxes, HitboxSystem.Level(position), navigationService.CellPixels);
    }

    // a distance in tiles from the player, an entity or the middle of a tile
    private NavGoal? AwayGoal(MoveAwayMovement away)
    {
        var distance = Math.Max(1, away.Distance) * Tile;
        if (away.TargetType == MovementTarget.Location) return new AwayGoal((away.TargetX + 0.5) * Tile, (away.TargetY + 0.5) * Tile, distance);
        if (Other(away.TargetType == MovementTarget.Player, away.TargetEntityId) is not { } other) return null;

        var boxes = HitboxesOf(other);
        if (boxes.Length == 0) return null;
        return new AwayGoal(boxes[0].X + (boxes[0].Width / 2.0), boxes[0].Y + (boxes[0].Height / 2.0), distance);
    }

    private MapHitbox[] HitboxesOf(MapEntity entity)
    {
        if (entity.GetModule<EntityHitboxModule>() is not { } hitboxModule || entity.GetModule<PositionModule>() is not { } position) return [];
        var state = entity.State<HitboxState>();
        return HitboxSystem.Areas(hitboxModule, position.Direction).Select(state.Get).ToArray();
    }

    #endregion

    #region Private Methods

    // the player, or an entity by its id on the map
    private MapEntity? Other(bool player, int? id)
    {
        if (player) return gameStateService.Player;
        return id is { } mapId ? gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Id == mapId) : null;
    }

    private static void Turn(MapEntity entity, PositionModule position, Direction direction)
    {
        if (entity.GetModule<MovementModule>() is { FixDirection: true }) return;
        position.Direction = direction;
    }

    // the way from the entity to the other by where their feet are, the way it already faces on the same spot
    private Direction Facing(MapEntity entity, PositionModule position, MapEntity other)
    {
        return hitboxSystem.Toward(entity, other) is var way and not Direction.None ? way : position.Direction;
    }

    // fades the entity to the opacity over the time, the update steps it along
    public void FadeTo(MapEntity entity, double opacity, int milliseconds)
    {
        var appearance = entity.State<AppearanceState>();
        var updates = Math.Max(1, gameDataService.Updates(milliseconds));
        appearance.TargetOpacity = Math.Clamp(opacity, 0, 1);
        appearance.OpacityPerUpdate = Math.Abs(appearance.TargetOpacity - appearance.Opacity) / updates;
    }

    private static void UpdateAppearance(MapEntity entity)
    {
        var appearance = entity.State<AppearanceState>();
        appearance.Tint.Step();
        if (appearance.Opacity == appearance.TargetOpacity) return;

        var step = Math.Max(appearance.OpacityPerUpdate, 0.0001);
        appearance.Opacity = appearance.Opacity < appearance.TargetOpacity
            ? Math.Min(appearance.TargetOpacity, appearance.Opacity + step)
            : Math.Max(appearance.TargetOpacity, appearance.Opacity - step);
    }

    // switches, variables and self switches, like control flow on a page
    private void Flow(MapEntity entity, FlowMovement flow)
    {
        if (!string.IsNullOrEmpty(flow.SwitchName) && flow.SwitchValue is { } switchValue) gameStateService.Switches[flow.SwitchName] = switchValue;
        if (!string.IsNullOrEmpty(flow.VariableName)) gameStateService.Variables[flow.VariableName] = flow.VariableValue ?? string.Empty;
        if (flow.SelfSwitchKey is { } key && bool.TryParse(flow.SelfSwitchValue, out var selfValue))
        {
            var owner = flow.SelfSwitchEntity is { } id ? gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Id == id) : entity;
            if (owner is not null) gameStateService.SetSelfSwitch(owner, key, selfValue);
        }
    }

    private static Direction Opposite(Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => direction
    };

    #endregion

    #region Types

    private enum StepResult
    {
        Done,
        Busy,
        Blocked
    }

    #endregion
}
