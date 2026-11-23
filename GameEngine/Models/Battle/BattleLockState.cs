namespace GameEngine.Models.Battle;

/// <summary>
/// Kept on a fighter's body in battle: locked while it casts (a cast can't be cancelled and the caster can't move) and
/// while it's down, and stunned while it holds a stun-lock state. Whatever moves the body (the player's keys now, the AI
/// later) checks it first.
/// </summary>
public class BattleLockState
{
    public bool Locked { get; set; }

    // holding a stun-lock state: no walking until it's gone
    public bool Stunned { get; set; }

    public bool CannotMove => Locked || Stunned;
}
