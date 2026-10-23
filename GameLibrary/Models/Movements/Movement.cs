using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Movements;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(BlendModeMovement), "blend_movement")]
[JsonDerivedType(typeof(CharacterImageMovement), "character_image_movement")]
[JsonDerivedType(typeof(DirectionFixMovement), "direction_fix_movement")]
[JsonDerivedType(typeof(FlowMovement), "flow_movement")]
[JsonDerivedType(typeof(MoveAwayMovement), "move_away_movement")]
[JsonDerivedType(typeof(MoveMovement), "move_movement")]
[JsonDerivedType(typeof(MoveRandomMovement), "move_random_movement")]
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
[JsonDerivedType(typeof(TurnRandomMovement), "turn_random_movement")]
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

    #endregion

    #region Overrides

    public abstract override string ToString();

    #endregion
}
