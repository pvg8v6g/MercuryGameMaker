namespace GameLibrary.Models.Movements;

/// <summary>
/// When reached during a projectile's movement route, the projectile lets off the
/// selected arte at its current location. The parent of that arte is set to the
/// fighter who cast the original arte (for damage multipliers and such).
/// The actual arte activation is handled by the game engine.
/// </summary>
public partial class ActivateArteMovement : Movement
{
    public Guid? ArteId
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Activate Arte";
}
