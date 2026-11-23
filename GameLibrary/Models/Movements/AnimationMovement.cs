namespace GameLibrary.Models.Movements;

/// <summary>
/// Plays an animation on the entity. With Wait For Completion the route holds until it has played through, so a step
/// after it (a projectile's Detonate) happens the moment the animation ends.
/// </summary>
public partial class AnimationMovement : Movement
{
    #region Properties

    public Guid? AnimationId
    {
        get;
        set => SetField(ref field, value);
    }

    // the picked animation's name, for the route list
    public string? AnimationName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    } = true;

    #endregion

    public override string ToString() => $"Play Animation: {AnimationName ?? "None"}{(WaitForCompletion ? " (Wait)" : string.Empty)}";
}
