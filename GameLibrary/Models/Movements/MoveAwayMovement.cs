namespace GameLibrary.Models.Movements;

public partial class MoveAwayMovement : Movement
{
    /// <summary>When set the movement moves away from the player instead of an entity or location.</summary>
    public bool TargetPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to move away from. When null (and not targeting the player) the location is used.</summary>
    public Guid? TargetEntityId
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

    public override string ToString()
    {
        if (TargetPlayer) return "Move Away From Player";
        if (TargetEntityId != null) return "Move Away From Entity";
        return $"Move Away From Location: {TargetX}, {TargetY}";
    }
}
