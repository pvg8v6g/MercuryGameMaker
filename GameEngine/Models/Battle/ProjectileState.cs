namespace GameEngine.Models.Battle;

/// <summary>
/// What a projectile's route asked the battle to do since the battle last looked. The route system only notes the
/// Detonate and Activate Arte steps here, and the battle runner acts on them after the systems have run, so nothing is
/// added to or taken off the map while the systems are walking through it. Only an arte projectile in a battle has
/// IsProjectile set, so on the story maps those two steps are still skipped.
/// </summary>
public class ProjectileState
{
    public bool IsProjectile { get; set; }

    // the route reached a Detonate step: the route stops there and the projectile goes off
    public bool Detonated { get; set; }

    // Activate Arte steps reached, in order, each one goes off where the projectile is and the projectile keeps going
    public Queue<Guid> Activations { get; } = [];

    // Place Randomly moved it in one jump, so the next contact check starts fresh from where it landed
    public bool Jumped { get; set; }
}
