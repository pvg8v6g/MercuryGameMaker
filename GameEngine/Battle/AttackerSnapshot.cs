using GameEngine.Models.Battle;

namespace GameEngine.Battle;

/// <summary>
/// Everything the hit order reads from the attacker's side, frozen at one moment: a normal hit makes a fresh one, and a
/// projectile or a state keeps the one made when it was fired or applied. Source is the live fighter, for who and where
/// (Threat goes to them, a taunt points at them).
/// </summary>
public record AttackerSnapshot(BattleFighter Source, int Team, int Level, double Power, double Accuracy, double Critical);
