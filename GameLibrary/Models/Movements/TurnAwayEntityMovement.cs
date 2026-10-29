namespace GameLibrary.Models.Movements;

public partial class TurnAwayEntityMovement : Movement
{
    /// <summary>When set the entity turns away from the player instead of another entity.</summary>
    public bool TargetPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to turn away from. When null (and not targeting the player) nothing is targeted.</summary>
    public int? TargetEntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => TargetPlayer ? "Turn Away From Player" : "Turn Away From Entity";
}
