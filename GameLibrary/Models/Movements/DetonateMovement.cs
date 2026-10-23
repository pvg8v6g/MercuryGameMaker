namespace GameLibrary.Models.Movements;

/// <summary>
/// When reached during a projectile's movement route, triggers the parent arte's
/// damage and states to immediately go off at the projectile's location.
/// The actual damage/state application is handled by the game engine.
/// </summary>
public partial class DetonateMovement : Movement
{
    public override string ToString() => "Detonate";
}
