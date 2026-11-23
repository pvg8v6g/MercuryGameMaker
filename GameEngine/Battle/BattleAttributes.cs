using GameEngine.Models.Battle;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle;

/// <summary>
/// A fighter's attributes in battle: what its class growths gave it (Fighter.Attributes) plus what its equipment adds.
/// The battle formulas look the attributes up by name. (Later this should be a setting so other games can name their
/// attributes anything; for now these are the names on the Attributes page.)
/// </summary>
public class BattleAttributes(IGameDataService gameDataService)
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

        var value = (double) fighter.Stats.Attributes.GetValueOrDefault(attribute.Guid);
        foreach (var slot in fighter.Stats.Equipment.Values)
        {
            if (slot.Id is not { } id || gameDataService.Equipment.FirstOrDefault(x => x.Guid == id) is not { } equipment) continue;
            value += equipment.AttributeStats.GetValueOrDefault(attribute.Guid);
        }

        return Math.Max(0, value);
    }

    // the attacker's numbers frozen now, for a hit now or for whatever it set off later (a projectile, a state)
    public AttackerSnapshot Snapshot(BattleFighter fighter)
    {
        return new AttackerSnapshot(fighter, fighter.Team, Math.Max(1, fighter.Stats.Level), Value(fighter, Power), Value(fighter, Accuracy), Value(fighter, Critical));
    }

    #endregion
}
