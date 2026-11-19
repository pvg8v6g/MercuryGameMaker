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

    /// <summary>How far to get from the target in tiles. The entity stops sooner when it can't get any farther.</summary>
    public int Distance
    {
        get;
        set => SetField(ref field, value);
    } = 5;

    public override string ToString() => TargetType switch
    {
        MovementTarget.Player => $"Move Away From Player: {Distance} tiles",
        MovementTarget.Entity => $"Move Away From Entity {TargetEntityId:000}: {Distance} tiles",
        _ => $"Move Away From Location: {TargetX}, {TargetY}, {Distance} tiles"
    };
}
