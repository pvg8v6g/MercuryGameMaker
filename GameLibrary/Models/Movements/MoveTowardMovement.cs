namespace GameLibrary.Models.Movements;

public partial class MoveTowardMovement : Movement
{
    /// <summary>When set the movement targets the player instead of an entity or location.</summary>
    public bool TargetPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to move toward. When null (and not targeting the player) the location is used.</summary>
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
        if (TargetPlayer) return "Move Toward Player";
        if (TargetEntityId != null) return "Move Toward Entity";
        return $"Move Toward Location: {TargetX}, {TargetY}";
    }
}
