using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Balloons;
using GameLibrary.Models.Movements;
using GameMaker.UX.Models.Choices;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.MovementEditor;

public class MovementEditorViewModel : PropertyChangedUpdater
{
    #region Fields

    private readonly TargetChoice[] _targets;
    private readonly NamedChoice[] _balloons;
    private readonly int _stepsPerTile;

    #endregion

    #region Construction

    // the map's entities are who a step can move, the balloons come from the Balloon Icons settings,
    // stepsPerTile (the game's Division) turns a move step's walking steps into tiles
    public MovementEditorViewModel(Movement movement, ObservableCollection<Arte> artes, IEnumerable<Entity> mapEntities, IEnumerable<BalloonIcon> balloons, int stepsPerTile)
    {
        Movement = movement;
        _stepsPerTile = Math.Max(1, stepsPerTile);
        Artes = artes;
        _targets = TargetChoice.List(mapEntities);
        _balloons = NamedChoice.List("None", balloons.AsEnumerable().OrderBy(x => x.Id));

        // move toward and away only pick an entity while their target is an entity
        movement.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MoveTowardMovement.TargetType)) OnPropertyChanged(nameof(ShowsOtherEntity));
        };
    }

    #endregion

    #region Properties

    public Movement Movement { get; }

    public ObservableCollection<Arte> Artes { get; }

    // waits, sounds and switches don't move anyone, they get no entity to pick
    public bool ShowsTarget => Movement.HasEntityTarget;

    public bool ShowsDirection => Movement is MoveMovement or TurnMovement;

    // move once, step forward and step backward walk a number of walking steps
    public bool ShowsSteps => Movement is StepCountMovement;

    public int Steps
    {
        get => Movement is StepCountMovement step ? step.Steps : 0;
        set
        {
            if (Movement is not StepCountMovement step || step.Steps == value) return;
            step.Steps = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TilesText));
        }
    }

    // how far the steps go on the grid, so 60 walking steps reads as 5 tiles
    public string TilesText
    {
        get
        {
            var tiles = Steps / (double)_stepsPerTile;
            return $"= {tiles:0.##} {(tiles == 1 ? "tile" : "tiles")} ({_stepsPerTile} steps make one tile)";
        }
    }

    public bool ShowsTurnTarget => Movement is TurnTowardEntityMovement or TurnAwayEntityMovement;

    public bool ShowsBalloon => Movement is BalloonMovement;

    public bool ShowsArte => Movement is ActivateArteMovement;

    public bool ShowsOtherEntity => Movement is MoveTowardMovement { TargetType: MovementTarget.Entity } or MoveAwayMovement { TargetType: MovementTarget.Entity };

    public Arte? SelectedArte
    {
        get => Movement is ActivateArteMovement arte ? Artes.FirstOrDefault(x => x.Guid == arte.ArteId) : null;
        set
        {
            if (value is null || value == SelectedArte || Movement is not ActivateArteMovement arte) return;
            arte.ArteId = value.Guid;
            OnPropertyChanged();
        }
    }

    // the entity the route belongs to, the player, then every entity on the map
    public TargetChoice[] TargetChoices => _targets;

    public TargetChoice SelectedTarget
    {
        get => TargetChoice.Find(_targets, Movement);
        set
        {
            // a dropdown clears its pick (null) when it reloads, that never changes the step
            if (value is null || value == SelectedTarget) return;
            Movement.TargetsPlayer = value.Player;
            Movement.EntityId = value.Id;
            Movement.TargetName = value.Id is null && !value.Player ? null : value.Name;
            OnPropertyChanged();
        }
    }

    // move toward and move away: the entity to go to or get away from, when the target is an entity
    public TargetChoice[] OtherEntities => _targets[2..];

    public TargetChoice? SelectedOtherEntity
    {
        get => Movement switch
        {
            MoveTowardMovement toward => ByMapId(OtherEntities, toward.TargetEntityId),
            MoveAwayMovement away => ByMapId(OtherEntities, away.TargetEntityId),
            _ => null
        };
        set
        {
            if (value is null || value == SelectedOtherEntity) return;
            if (Movement is MoveTowardMovement toward) toward.TargetEntityId = value.MapId;
            if (Movement is MoveAwayMovement away) away.TargetEntityId = value.MapId;
            OnPropertyChanged();
        }
    }

    // turn toward and turn away: the player or an entity on the map
    public TargetChoice[] TurnTargets => _targets[1..];

    public TargetChoice? SelectedTurnTarget
    {
        get => Movement switch
        {
            TurnTowardEntityMovement toward => toward.TargetPlayer ? TurnTargets[0] : ByMapId(TurnTargets, toward.TargetEntityId),
            TurnAwayEntityMovement away => away.TargetPlayer ? TurnTargets[0] : ByMapId(TurnTargets, away.TargetEntityId),
            _ => null
        };
        set
        {
            if (value is null || value == SelectedTurnTarget) return;
            if (Movement is TurnTowardEntityMovement toward)
            {
                toward.TargetPlayer = value.Player;
                toward.TargetEntityId = value.MapId;
            }

            if (Movement is TurnAwayEntityMovement away)
            {
                away.TargetPlayer = value.Player;
                away.TargetEntityId = value.MapId;
            }

            OnPropertyChanged();
        }
    }

    // move once and turn: the four ways or a random one each time, no "none"
    public string[] DirectionChoices { get; } = [nameof(Direction.Down), nameof(Direction.Left), nameof(Direction.Right), nameof(Direction.Up), RandomText];

    public string? SelectedDirection
    {
        get => Movement switch
        {
            MoveMovement move => DirectionText(move.Random, move.Direction),
            TurnMovement turn => DirectionText(turn.Random, turn.Direction),
            _ => null
        };
        set
        {
            // the dropdown echoes the pick back when it's told about it, only a real change goes through
            if (value is null || value == SelectedDirection) return;
            var random = value == RandomText;
            var direction = random ? Direction.Down : Enum.Parse<Direction>(value);
            if (Movement is MoveMovement move)
            {
                move.Random = random;
                move.Direction = direction;
            }

            if (Movement is TurnMovement turn)
            {
                turn.Random = random;
                turn.Direction = direction;
            }

            OnPropertyChanged();
        }
    }

    public NamedChoice[] BalloonChoices => _balloons;

    public NamedChoice? SelectedBalloon
    {
        get => Movement is BalloonMovement balloon ? NamedChoice.Find(_balloons, balloon.BalloonIconId) : null;
        set
        {
            if (value is null || value == SelectedBalloon || Movement is not BalloonMovement balloon) return;
            balloon.BalloonIconId = value.Id;
            balloon.BalloonIconName = value.Id is null ? null : value.Name;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Private Methods

    private const string RandomText = "Random";

    // the dropdown's own string for the step (a new string the dropdown can't match would loop forever),
    // a step saved with no direction shows as down, the way it was drawn
    private string DirectionText(bool random, Direction direction)
    {
        if (random) return DirectionChoices[^1];
        var text = direction == Direction.None ? nameof(Direction.Down) : direction.ToString();
        return DirectionChoices.First(x => x == text);
    }

    // nothing picked yet (or an entity since deleted) shows no pick
    private static TargetChoice? ByMapId(TargetChoice[] choices, int? mapId)
    {
        return mapId is null ? null : choices.FirstOrDefault(x => !x.Player && x.MapId == mapId);
    }

    #endregion
}
