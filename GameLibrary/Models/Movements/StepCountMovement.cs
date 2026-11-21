namespace GameLibrary.Models.Movements;

/// <summary>
/// A step that walks a set distance one way. Steps is counted in walking steps (the grid divided by the game's
/// Division, 4 px with a 48 px grid and a Division of 12), the same small steps a character takes when walked with the
/// keys, so a route can move any distance and not only whole tiles.
/// </summary>
public abstract partial class StepCountMovement : Movement
{
    #region Properties

    public int Steps
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 1;

    #endregion
}
