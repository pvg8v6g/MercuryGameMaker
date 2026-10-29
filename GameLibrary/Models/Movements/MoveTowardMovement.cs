using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class MoveTowardMovement : Movement
{
    /// <summary>Determines whether the movement targets the player, an entity, or a location. These options are mutually exclusive.</summary>
    public MovementTarget TargetType
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to move toward. When null (and not targeting the player) the location is used.</summary>
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
        MovementTarget.Player => "Move Toward Player",
        MovementTarget.Entity => "Move Toward Entity",
        _ => $"Move Toward Location: {TargetX}, {TargetY}"
    };
}
