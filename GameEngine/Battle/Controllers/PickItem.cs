using GameEngine.Models.Battle;
using GameLibrary.Models.Items;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// Which of the party's items a fighter allowed to use them would use right now. Items are shared, few, and on one
/// cooldown, so it only uses what's needed badly: getting someone up, Life or Mana nearly gone, or a state that stops or
/// wears down an ally. Boosts and thrown items are left to the player.
/// </summary>
public class PickItem(BattleStates states)
{
    #region Constants

    // Life shares: one ally this low, or two this low
    private const double OneBelow = 0.35;
    private const double TwoBelow = 0.5;

    private const double ManaBelow = 0.2;

    #endregion

    #region Methods

    // the item slot to use, null for none
    public int? Pick(BattleFighter fighter, IBattleActions battle)
    {
        if (!fighter.Stats.CanUseItems) return null;

        for (var i = 0; i < battle.ItemSlots.Count; i++)
        {
            if (battle.ItemSlots[i] is { } item && battle.CanUseItem(fighter, i) && IsNeeded(fighter, battle, item)) return i;
        }

        return null;
    }

    #endregion

    #region Private Methods

    private bool IsNeeded(BattleFighter fighter, IBattleActions battle, Consumable item)
    {
        if (item.HpDamage > 0 || item.HpPercent > 0) return false;

        var friends = battle.Fighters.Where(x => !battle.IsOpponent(fighter, x) && battle.ItemWouldHit(fighter, item, x)).ToArray();
        if (friends.Any(x => x.IsDown)) return true;

        if ((item.HpDamage < 0 || item.HpPercent < 0) &&
            (friends.Any(x => LifeShare(x) < OneBelow) || friends.Count(x => LifeShare(x) < TwoBelow) >= 2)) return true;

        if ((item.ManaDamage < 0 || item.ManaPercent < 0) && friends.Any(x => x.Stats.MaxMana > 0 && x.Stats.Mana < x.Stats.MaxMana * ManaBelow)) return true;

        return friends.Any(x => x.States.Any(held => IsBad(held) && Removes(item, held)));
    }

    // stops its holder or wears it down
    private static bool IsBad(BattleState held) => !held.State.Friendly && (held.State.StunLock || held.State.Silence || held.State.TickArte is not null);

    private bool Removes(Consumable item, BattleState held)
    {
        return held.State.Guid != states.IncapacitatedId && (item.RemoveNegativeStates || item.RemoveStates.Contains(held.State.Guid));
    }

    private static double LifeShare(BattleFighter fighter) => fighter.Stats.MaxLife <= 0 ? 1 : (double) fighter.Stats.Life / fighter.Stats.MaxLife;

    #endregion
}
