using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class MoveAwayMovement : Movement
{
    /// <summary>Determines whether the movement moves away from the player, an entity, or a location. These options are mutually exclusive.</summary>
    public MovementTarget TargetType
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to move away from. When null (and not targeting the player) the location is used.</summary>
    public int? TargetEntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public int TargetX
    {
        get;
        set => SetField(ref field, value);
    }

    public int TargetY
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => TargetType switch
    {
        MovementTarget.Player => "Move Away From Player",
        MovementTarget.Entity => "Move Away From Entity",
        _ => $"Move Away From Location: {TargetX}, {TargetY}"
    };
}
