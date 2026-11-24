using GameEngine.Models.Battle;
using GameEngine.Services.GameState;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle;

/// <summary>
/// The states on the party outside battle. A state keeps its clock running wherever the party is: one put on a member from
/// the menu (a stim, a salve) wears down on the story map, a state marked Remains After Battle comes out of a fight still
/// on, and either way whatever is left of one goes into the next fight with them. Time only passes while the game runs, so
/// a menu or a message holds it, and a party member that isn't in a fight keeps their clock where it was until it ends.
/// Outside battle a state's only effects are its clock and its Tick Arte, worked out without a battlefield: the arte's
/// Life amount (its multiplier times the numbers of whoever applied the state) heals or hurts the holder, and a hurt never
/// takes the holder below 1 Life. Everything else a state does (attributes, speed, stuns) only matters in a battle.
/// </summary>
public class PartyStates(IGameDataService gameDataService, IGameStateService gameStateService, BattleStates states, BattleAttributes attributes)
{
    #region Methods

    // puts a state on a party member (from an item used in the menu, say). The source is who gets credit for its numbers,
    // the holder when nobody else is given. Returns whether it went on
    public bool Add(Fighter holder, Guid stateId, Fighter? source = null)
    {
        if (stateId == states.IncapacitatedId || holder.Life <= 0) return false;
        if (gameDataService.States.FirstOrDefault(x => x.Guid == stateId) is not { } state) return false;

        var held = gameStateService.CarriedStates.GetValueOrDefault(holder.Guid) ?? [];
        if (states.Resists(holder, held.Select(x => x.State), stateId)) return false;

        if (held.FirstOrDefault(x => x.State.Guid == stateId) is { } existing)
        {
            if (!state.Stackable) return false;

            existing.Stacks = Math.Min(Math.Max(1, state.MaxStacks), existing.Stacks + 1);
            existing.Left = BattleState.Frames(state);
            return true;
        }

        source ??= holder;
        held.Add(new CarriedState(
            state,
            Math.Max(1, source.Level),
            attributes.Base(source, BattleAttributes.Power),
            attributes.Base(source, BattleAttributes.Accuracy),
            attributes.Base(source, BattleAttributes.Critical),
            source.Guid));
        gameStateService.CarriedStates[holder.Guid] = held;
        return true;
    }

    public bool Remove(Fighter holder, Guid stateId) => gameStateService.CarriedStates.GetValueOrDefault(holder.Guid)?.RemoveAll(x => x.State.Guid == stateId) > 0;

    // every state that isn't Friendly goes, as in a battle
    public bool RemoveNegative(Fighter holder) => gameStateService.CarriedStates.GetValueOrDefault(holder.Guid)?.RemoveAll(x => !x.State.Friendly) > 0;

    // one update on the story map: every state's clock runs down, and the ones due tick
    public void Tick()
    {
        if (gameStateService.CarriedStates.Count == 0) return;

        foreach (var (id, held) in gameStateService.CarriedStates.ToArray())
        {
            if (held.Count == 0)
            {
                gameStateService.CarriedStates.Remove(id);
                continue;
            }

            if (!gameStateService.Actors.TryGetValue(id, out var actor)) continue;

            foreach (var each in held.ToArray())
            {
                var state = each.State;
                var expired = state.Ticks > 0 && --each.Left <= 0;
                if (state.TickArte is not null && --each.NextTick <= 0)
                {
                    each.NextTick = Math.Max(1, state.TickInterval);
                    TickOn(actor, each);
                }

                if (expired) held.Remove(each);
            }
        }
    }

    // a battle begins: the party members in it start with the states they carry, what's left on each clock
    public void CarryIn(IReadOnlyList<BattleFighter> fighters)
    {
        foreach (var fighter in fighters.Where(x => !x.IsEnemy))
        {
            fighter.States.Clear();
            foreach (var each in gameStateService.CarriedStates.GetValueOrDefault(fighter.Stats.Guid) ?? [])
            {
                var source = fighters.FirstOrDefault(x => !x.IsEnemy && x.Stats.Guid == each.SourceActor) ?? fighter;
                var attacker = new AttackerSnapshot(source, BattleFighter.PartyTeam, each.Level, each.Power, each.Accuracy, each.Critical);
                fighter.States.Add(new BattleState(each.State, attacker) { Stacks = each.Stacks, Left = each.Left, NextTick = each.NextTick });
            }
        }
    }

    // a battle ends: what's still on each party member in it (the battle already took away what doesn't remain) is carried out
    public void CarryOut(IEnumerable<BattleFighter> fighters)
    {
        foreach (var fighter in fighters.Where(x => !x.IsEnemy))
        {
            var carried = fighter.States.Select(x => new CarriedState(x.State, x.Attacker.Level, x.Attacker.Power, x.Attacker.Accuracy, x.Attacker.Critical, x.Source.IsEnemy ? null : x.Source.Stats.Guid)
            {
                Stacks = x.Stacks,
                Left = x.Left,
                NextTick = x.NextTick
            }).ToList();

            if (carried.Count == 0) gameStateService.CarriedStates.Remove(fighter.Stats.Guid);
            else gameStateService.CarriedStates[fighter.Stats.Guid] = carried;
        }
    }

    // a copy of what everyone carries now, for a retry to put back
    public Dictionary<Guid, List<CarriedState>> Snapshot() => gameStateService.CarriedStates.ToDictionary(x => x.Key, x => x.Value.Select(y => y.Copy()).ToList());

    public void Restore(Dictionary<Guid, List<CarriedState>> snapshot)
    {
        gameStateService.CarriedStates.Clear();
        foreach (var (id, held) in snapshot) gameStateService.CarriedStates[id] = [.. held.Select(x => x.Copy())];
    }

    #endregion

    #region Private Methods

    // the Tick Arte on the holder, no hitboxes or animation: its multiplier times the numbers of whoever applied the state,
    // against the holder's Defense for damage, spread by the arte's Variance, never taking the holder below 1 Life
    private void TickOn(Fighter holder, CarriedState each)
    {
        if (holder.Life <= 0 || gameDataService.Artes.FirstOrDefault(x => x.Guid == each.State.TickArte) is not { HpDamage: not 0 } arte) return;

        var heals = arte.HpDamage < 0;
        var value = Math.Abs(arte.HpDamage) * each.Power;
        if (!heals)
        {
            var defense = attributes.Base(holder, BattleAttributes.Defense);
            value *= each.Power + defense <= 0 ? 0 : each.Power / (each.Power + defense);
        }

        var variance = Math.Clamp(arte.Variance / 100.0, 0, 0.5);
        value *= 1 + (((Random.Shared.NextDouble() * 2) - 1) * variance);
        var amount = (int) Math.Round(value);
        holder.Life = Math.Clamp(holder.Life + (heals ? amount : -amount), 1, Math.Max(1, holder.MaxLife));
    }

    #endregion
}
