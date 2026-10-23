namespace GameLibrary.Models.Movements;

public partial class TurnTowardEntityMovement : Movement
{
    /// <summary>When set the entity turns toward the player instead of another entity.</summary>
    public bool TargetPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to turn toward. When null (and not targeting the player) nothing is targeted.</summary>
    public Guid? TargetEntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => TargetPlayer ? "Turn Toward Player" : "Turn Toward Entity";
}
