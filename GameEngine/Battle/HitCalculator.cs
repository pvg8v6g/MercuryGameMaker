using GameEngine.Models.Battle;
using GameLibrary.Models.Artes;

namespace GameEngine.Battle;

/// <summary>
/// Works out one arte landing on one fighter, in the order from the battle doc (Docs\BattleSystem.md, The order of a
/// hit): hit or miss, (states come here later), the base value, the Defense ratio, elements, critical, Variance, rounding.
/// Applying it (Life, Threat, falling) is the battle runner's job.
/// </summary>
public class HitCalculator(BattleAttributes attributes)
{
    #region Constants

    // equal Accuracy and Evasion hit this often, and nothing is ever harder to hit than the floor
    private const double HitScale = 1.8;
    private const double HitFloor = 0.05;

    // damage from behind (the target faces away from where the hit came from) does this many times the damage
    private const double BehindMultiplier = 1.25;

    // a critical hit does this many times the damage or healing
    private const double CriticalMultiplier = 3.0;

    // Variance never spreads a hit by more than this share either way
    private const double MaxVariance = 0.5;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Methods

    // afterHit runs once the hit roll is passed and before any damage is worked out: the arte's states go on then, so
    // they count toward its own damage
    public HitOutcome Resolve(Arte arte, AttackerSnapshot attacker, BattleFighter target, Action? afterHit = null, bool fromBehind = false)
    {
        var heals = arte.HpDamage < 0;

        // 2. hit or miss: healing, and anything landing on the attacker's own side, never misses
        var friendly = target.OriginalTeam == attacker.Team;
        if (!heals && !friendly)
        {
            var accuracy = attacker.Accuracy;
            var evasion = attributes.Value(target, BattleAttributes.Evasion);
            var chance = accuracy + evasion <= 0 ? 1.0 : Math.Clamp(HitScale * accuracy / (accuracy + evasion), HitFloor, 1.0);
            if (_random.NextDouble() >= chance) return HitOutcome.Miss;
        }

        // 3. and 4. remove and add states
        afterHit?.Invoke();

        // 5. the arte's multiplier times the attacker's Power (1 is 1x Power), 6. the Defense ratio, skipped for healing
        var value = Math.Abs(arte.HpDamage) * attacker.Power;
        if (!heals)
        {
            var defense = attributes.Value(target, BattleAttributes.Defense);
            value *= attacker.Power + defense <= 0 ? 0 : attacker.Power / (attacker.Power + defense);
        }

        // 7. elements: every element's change from 100% is added up. A heal never turns into damage
        var rate = ElementRate(arte, target);
        if (heals && rate < 0) rate = 0;
        value *= rate / 100.0;

        // 7b. a hit on an opponent from behind: a bit more damage (never healing, never a friendly)
        if (fromBehind && !heals && !friendly) value *= BehindMultiplier;

        // 8. critical
        var critical = _random.NextDouble() < CriticalChance(attacker);
        if (critical) value *= CriticalMultiplier;

        // 9. Variance, a share of the hit either way
        var variance = Math.Clamp(arte.Variance / 100.0, 0, MaxVariance);
        value *= 1 + ((_random.NextDouble() * 2) - 1) * variance;

        // 10. rounded, a 0 stays 0. A negative result is absorbed (it heals the target)
        var amount = (int) Math.Round(value);
        return new HitOutcome(false, heals ? -amount : amount, critical);
    }

    #endregion

    #region Private Methods

    // rate = 100% + the sum of (each element's rate - 100%); no elements is a plain 100%
    private double ElementRate(Arte arte, BattleFighter target)
    {
        if (arte.Elements.Count == 0) return 100;

        return 100 + arte.Elements.Sum(element => attributes.ElementRate(target, element) - 100.0);
    }

    // crits stay rare: the best critter in the party tops out near 10%
    private static double CriticalChance(AttackerSnapshot attacker)
    {
        return attacker.Critical <= 0 ? 0 : attacker.Critical / (attacker.Critical + 250 + (44.0 * attacker.Level));
    }

    #endregion
}

// a hit's result: missed, or how much Life it takes (positive) or gives (negative), and whether it was critical
public record HitOutcome(bool Missed, int LifeChange, bool Critical)
{
    public static HitOutcome Miss { get; } = new(true, 0, false);
}
