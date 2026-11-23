namespace GameEngine.Models.Battle;

/// <summary>
/// Kept on a fighter's body in battle: locked while it casts (a cast can't be cancelled and the caster can't move) and
/// while it's down. Whatever moves the body (the player's keys now, the AI later) checks it first.
/// </summary>
public class BattleLockState
{
    public bool Locked { get; set; }
}
