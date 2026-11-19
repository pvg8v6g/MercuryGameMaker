using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Movements;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(BalloonMovement), "balloon_movement")]
[JsonDerivedType(typeof(BlendModeMovement), "blend_movement")]
[JsonDerivedType(typeof(CharacterImageMovement), "character_image_movement")]
[JsonDerivedType(typeof(DirectionFixMovement), "direction_fix_movement")]
[JsonDerivedType(typeof(FlowMovement), "flow_movement")]
[JsonDerivedType(typeof(MoveAwayMovement), "move_away_movement")]
[JsonDerivedType(typeof(MoveMovement), "move_movement")]
[JsonDerivedType(typeof(MoveTowardMovement), "move_toward_movement")]
[JsonDerivedType(typeof(OpacityMovement), "opacity_movement")]
[JsonDerivedType(typeof(PlayMediaMovement), "play_media_movement")]
[JsonDerivedType(typeof(SpeedMovement), "speed_movement")]
[JsonDerivedType(typeof(StepAwayMovement), "step_away_movement")]
[JsonDerivedType(typeof(StepForwardMovement), "step_forward_movement")]
[JsonDerivedType(typeof(SteppingMovement), "stepping_movement")]
[JsonDerivedType(typeof(ThroughMovement), "through_movement")]
[JsonDerivedType(typeof(Turn180Movement), "turn_180_movement")]
[JsonDerivedType(typeof(TurnAwayEntityMovement), "turn_away_movement")]
[JsonDerivedType(typeof(TurnMovement), "turn_movement")]
[JsonDerivedType(typeof(TurnTowardEntityMovement), "turn_toward_movement")]
[JsonDerivedType(typeof(WaitMovement), "wait_movement")]
[JsonDerivedType(typeof(DetonateMovement), "detonate_movement")]
[JsonDerivedType(typeof(ActivateArteMovement), "activate_arte_movement")]
public abstract partial class Movement : PropertyChangedUpdater
{
    #region Properties

    public Guid Guid
    {
        get;
        set => SetField(ref field, value);
    } = Guid.NewGuid();

    /// <summary>
    /// If this is null the movement affects the entity that owns the movement route.
    /// Otherwise the movement affects the entity with this id.
    /// </summary>
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    // the movement affects the player (the party leader) instead of an entity
    public bool TargetsPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    // the name of the entity picked, for the route list. None: the entity the route belongs to
    public string? TargetName
    {
        get;
        set => SetField(ref field, value);
    }

    // off for steps that don't act on an entity (waits, sounds, switches), they have no entity to pick
    [JsonIgnore]
    public virtual bool HasEntityTarget => true;

    // what the route list shows: the step, and who it moves when that isn't the route's own entity
    [JsonIgnore]
    public string DisplayText => HasEntityTarget && !string.IsNullOrEmpty(TargetName) ? $"{this} [{TargetName}]" : ToString();

    #endregion

    #region Construction

    // any change can change the text, so the route list always shows the step as it is
    protected Movement()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(DisplayText)) OnPropertyChanged(nameof(DisplayText));
        };
    }

    #endregion

    #region Overrides

    public abstract override string ToString();

    #endregion
}
