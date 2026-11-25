using GameEngine.Models.Battle;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle;

/// <summary>
/// The states on the fighters in a battle: adding them (resisted, stacked or ignored), removing them, wearing them
/// down, and finding which ones are due to tick. What a tick does (activate an arte) is up to the battle runner.
/// </summary>
public class BattleStates(IGameDataService gameDataService)
{
    #region Constants

    public const string IncapacitatedName = "Incapacitated";

    #endregion

    #region Methods

    public Guid IncapacitatedId => gameDataService.States.FirstOrDefault(x => x.Name == IncapacitatedName)?.Guid ?? Guid.Empty;

    // holding any state marked Stun-Lock
    public bool IsStunned(BattleFighter fighter) => fighter.States.Any(x => x.State.StunLock);

    // silenced fighters can't use artes, and neither can stunned ones unless every stun-lock state they hold lets them
    public bool BlocksArtes(BattleFighter fighter)
    {
        if (fighter.States.Any(x => x.State.Silence)) return true;

        var stuns = fighter.States.Where(x => x.State.StunLock).ToArray();
        return stuns.Length > 0 && stuns.Any(x => !x.State.CanUseArtes);
    }

    // a state that says No Items stops items, and so does a stun that stops artes (a fighter that can do nothing). A silence
    // or a stun that lets the holder use artes leaves items alone
    public bool BlocksItems(BattleFighter fighter)
    {
        return fighter.States.Any(x => x.State.NoItems) || fighter.States.Any(x => x.State.StunLock && !x.State.CanUseArtes);
    }

    // whoever taunted the fighter, the newest taunt first. A taunter that fell doesn't count
    public BattleFighter? TauntedBy(BattleFighter fighter) => fighter.States.LastOrDefault(x => x.State.Taunt && !x.Source.IsDown)?.Source;

    public bool CantBeTargeted(BattleFighter fighter) => fighter.States.Any(x => x.State.CantBeTargeted);

    // the arte puts a taunt on whoever it hits
    public bool Taunts(GameLibrary.Models.Artes.Arte arte) => gameDataService.States.Any(x => x.Taunt && arte.AddStates.Contains(x.Guid));

    public bool Has(BattleFighter fighter, Guid state) => fighter.States.Any(x => x.State.Guid == state);

    /// <summary>
    /// Puts a state on a fighter. Nothing can be added to an Incapacitated fighter, equipment can resist it, bosses
    /// resist stun-lock states, and one already there is ignored unless it stacks. Chance is the percent chance of it going
    /// on at all (100 always does). Returns whether it went on.
    /// </summary>
    public bool Add(BattleFighter fighter, Guid id, AttackerSnapshot attacker, int chance = 100)
    {
        if (id == IncapacitatedId || fighter.IsDown) return false;
        if (gameDataService.States.FirstOrDefault(x => x.Guid == id) is not { } state) return false;
        if (state.StunLock && fighter.IsBoss) return false;
        if (Resists(fighter.Stats, fighter.States.Select(x => x.State), id)) return false;
        if (chance < 100 && Random.Shared.Next(100) >= chance) return false;

        if (fighter.States.FirstOrDefault(x => x.State.Guid == id) is { } existing)
        {
            if (!state.Stackable) return false;

            existing.Stacks = Math.Min(Math.Max(1, state.MaxStacks), existing.Stacks + 1);
            existing.Left = BattleState.Frames(state);
            return true;
        }

        fighter.States.Add(new BattleState(state, attacker));
        return true;
    }

    /// <summary>
    /// The fighter falls: every state goes, and it becomes Incapacitated (which nothing can resist).
    /// </summary>
    public void Incapacitate(BattleFighter fighter, AttackerSnapshot attacker)
    {
        fighter.States.Clear();
        if (gameDataService.States.FirstOrDefault(x => x.Guid == IncapacitatedId) is { } state) fighter.States.Add(new BattleState(state, attacker));
    }

    public bool Remove(BattleFighter fighter, Guid id) => fighter.States.RemoveAll(x => x.State.Guid == id) > 0;

    // every state that isn't Friendly goes, except Incapacitated: curing ailments never gets a fallen fighter up
    public bool RemoveNegative(BattleFighter fighter)
    {
        var incapacitated = IncapacitatedId;
        return fighter.States.RemoveAll(x => !x.State.Friendly && x.State.Guid != incapacitated) > 0;
    }

    /// <summary>
    /// A fighter fell: the states it applied that end with their source go from everyone.
    /// </summary>
    public void SourceFell(IEnumerable<BattleFighter> everyone, BattleFighter source)
    {
        foreach (var fighter in everyone) fighter.States.RemoveAll(x => x.Source == source && x.State.EndsWhenSourceFalls);
    }

    /// <summary>
    /// The battle is over: everything goes except what says it remains. Incapacitated is always cleared.
    /// </summary>
    public void EndOfBattle(BattleFighter fighter)
    {
        var incapacitated = IncapacitatedId;
        fighter.States.RemoveAll(x => !x.State.RemainsAfterBattle || x.State.Guid == incapacitated);
    }

    /// <summary>
    /// One update: durations run down and the ones that ran out go. Returns the states that tick this update.
    /// </summary>
    public List<BattleState> Update(BattleFighter fighter)
    {
        var due = new List<BattleState>();
        foreach (var each in fighter.States.ToArray())
        {
            var state = each.State;
            var expired = state.Ticks > 0 && --each.Left <= 0;

            // a tick due on the very update the state runs out still counts, so 10 ticks at a 60 interval ticks 10 times
            if ((state.TickArte is not null || state.TickAnimation != Guid.Empty) && --each.NextTick <= 0)
            {
                each.NextTick = Math.Max(1, state.TickInterval);
                due.Add(each);
            }

            if (expired) fighter.States.Remove(each);
        }

        return due;
    }

    // what the fighter's states add to an attribute or change an element's rate by, for every stack
    public double AttributeChange(BattleFighter fighter, Guid attribute) => fighter.States.Sum(x => (double) x.State.AttributeChanges.GetValueOrDefault(attribute) * x.Stacks);

    // the percent the fighter's states change an attribute by, for every stack (-20 is 20% less)
    public double AttributePercent(BattleFighter fighter, Guid attribute) => fighter.States.Sum(x => (double) x.State.AttributePercents.GetValueOrDefault(attribute) * x.Stacks);

    // how fast the fighter walks: every state's Move Speed multiplied together (1 is normal)
    public double SpeedFactor(BattleFighter fighter) => fighter.States.Aggregate(1.0, (factor, x) => factor * x.State.MoveSpeed / 100.0);

    public double ElementChange(BattleFighter fighter, Guid element) => fighter.States.Sum(x => (x.State.ElementChanges.GetValueOrDefault(element, 100) - 100.0) * x.Stacks);

    #endregion

    #region Private Methods

    // whether a holder can't take the state: one it already holds keeps others off (Venom Ward keeps Poisoned off), and so can
    // its equipment. Used in battle and outside it
    public bool Resists(Fighter stats, IEnumerable<State> held, Guid id)
    {
        if (held.Any(x => x.ResistStates.Contains(id))) return true;

        foreach (var slot in stats.Equipment.Values)
        {
            if (slot.Id is not { } equipmentId || gameDataService.Equipment.FirstOrDefault(x => x.Guid == equipmentId) is not { } equipment) continue;
            if (equipment.ResistAllStates || equipment.ResistStates.Contains(id)) return true;
        }

        return false;
    }

    #endregion
}
