using GameEngine.Models.Battle;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle;

/// <summary>
/// A fighter's attributes in battle: what its class growths gave it (Fighter.Attributes) plus what its equipment adds.
/// The battle formulas look the attributes up by name. (Later this should be a setting so other games can name their
/// attributes anything; for now these are the names on the Attributes page.)
/// </summary>
public class BattleAttributes(IGameDataService gameDataService, BattleStates states)
{
    #region Constants

    public const string Power = "Power";
    public const string Defense = "Defense";
    public const string Accuracy = "Accuracy";
    public const string Evasion = "Evasion";
    public const string Critical = "Critical";
    public const string Concentration = "Concentration";

    #endregion

    #region Methods

    public double Value(BattleFighter fighter, string name)
    {
        if (gameDataService.Attributes.FirstOrDefault(x => x.Name == name) is not { } attribute) return 0;

        var value = Base(fighter.Stats, attribute.Guid);

        // flat changes first, then the percent ones, so a -20% Defense state means as much at level 50 as at level 1
        return Math.Max(0, (value + states.AttributeChange(fighter, attribute.Guid)) * (1 + (states.AttributePercent(fighter, attribute.Guid) / 100.0)));
    }

    // an actor's attribute outside battle: what its growths and equipment give it, with no states
    public double Base(Fighter stats, string name)
    {
        return gameDataService.Attributes.FirstOrDefault(x => x.Name == name) is { } attribute ? Base(stats, attribute.Guid) : 0;
    }

    private double Base(Fighter stats, Guid attribute)
    {
        var value = (double) stats.Attributes.GetValueOrDefault(attribute);
        foreach (var slot in stats.Equipment.Values)
        {
            if (slot.Id is not { } id || gameDataService.Equipment.FirstOrDefault(x => x.Guid == id) is not { } equipment) continue;
            value += equipment.AttributeStats.GetValueOrDefault(attribute);
        }

        return value;
    }

    // the fighter's total rate for one element: its own rate (100% when unset) plus what each piece of equipment changes
    // it by from 100%. Garrick at 100% Fire in 50% Fire armor ends at 50%. (and so do its states, like wards)
    public double ElementRate(BattleFighter fighter, Guid element)
    {
        var rate = (double) fighter.Stats.Elements.GetValueOrDefault(element, 100);
        foreach (var slot in fighter.Stats.Equipment.Values)
        {
            if (slot.Id is not { } id || gameDataService.Equipment.FirstOrDefault(x => x.Guid == id) is not { } equipment) continue;
            rate += equipment.ElementResist.GetValueOrDefault(element, 100) - 100;
        }

        return rate + states.ElementChange(fighter, element);
    }

    // the attacker's numbers frozen now, for a hit now or for whatever it set off later (a projectile, a state)
    public AttackerSnapshot Snapshot(BattleFighter fighter)
    {
        return new AttackerSnapshot(fighter, fighter.Team, Math.Max(1, fighter.Stats.Level), Value(fighter, Power), Value(fighter, Accuracy), Value(fighter, Critical));
    }

    #endregion
}
