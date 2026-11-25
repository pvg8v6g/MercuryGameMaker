using Windows.Foundation;
using GameEngine.Models.Battle;
using GameLibrary.Enumerations;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// The AI for everyone the player isn't driving, party members and enemies alike (Docs\BattleSystem.md, Pick me if and
/// the AI loop): choose a target and walk toward it, choose one of the artes whose "pick me if" passes, turn to face who
/// it's for, and use it. A healer goes to an ally that needs it first, everyone else picks by Threat.
/// Targeting has two speeds (Docs\BattleSystem.md, Threat): it checks every update and switches at once when its target
/// falls, but the weighted pick is only re-rolled every couple of seconds, so it commits to a chase.
/// </summary>
public class AiController(PickMeIf pickMeIf, PickItem pickItem, BattleWalk battleWalk, BattleStates states) : IFighterController
{
    #region Constants

    // updates between re-rolls of the weighted pick (about two seconds, plus a little so fighters don't all switch at once)
    private const int RerollEvery = 120;
    private const int RerollJitter = 40;

    // someone's Threat jumped: this many times the target's, this many times the most there was, and this much besides
    private const double JumpOverTarget = 2.0;
    private const double JumpOverTop = 1.5;
    private const double JumpBesides = 50;

    // the pause after an arte (updates), and how soon it looks again when nothing passed
    private const int ThinkMin = 15;
    private const int ThinkMax = 45;
    private const int LookAgain = 6;

    // a healer that can't get its heal to an ally in this many updates goes back to fighting for a while
    private const int HelpFor = 360;
    private const int HelpRest = 240;

    // in tiles: how far it walks from an enemy that's casting, and how far out it stays put instead of walking back in
    private const double FleeTiles = 3.5;
    private const double FleeHoldTiles = 4.5;

    // a temper (what lets a fighter use risky artes) lasts about five seconds. Losing Life can set it off, the bigger the
    // loss the likelier, and so can an ally falling. And now and then it's just in a mood
    private const int SpicyUpdates = 300;
    private const double SpicyPerLifeShare = 1.5;
    private const double SpicyAllyFell = 0.5;
    private const double SpicyMood = 0.05;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Methods

    public void Update(BattleFighter fighter, IBattleActions battle)
    {
        var state = fighter.Ai;
        Temper(fighter, battle);
        ChooseTarget(fighter, battle, state);
        Act(fighter, battle, state);
        Move(fighter, battle, state);
    }

    #endregion

    #region Private Methods

    private void ChooseTarget(BattleFighter fighter, IBattleActions battle, BattleAiState state)
    {
        if (state.HelpRest > 0) state.HelpRest--;

        // a taunted enemy goes after the taunter, whatever else it had in mind
        if (fighter.IsEnemy && states.TauntedBy(fighter) is { } taunter && battle.IsOpponent(fighter, taunter))
        {
            state.Target = taunter;
            state.RerollIn = 0;
            return;
        }

        // a healer goes to whoever needs it, as long as it isn't getting nowhere
        if (state.HelpRest <= 0 && pickMeIf.IsHealer(fighter) && pickMeIf.Needy(fighter, battle) is { } needy)
        {
            if (++state.HelpingFor <= HelpFor)
            {
                state.Target = needy;
                return;
            }

            state.HelpRest = HelpRest;
        }

        state.HelpingFor = 0;
        var candidates = battle.Fighters.Where(x => !x.IsDown && battle.IsOpponent(fighter, x) && !states.CantBeTargeted(x)).ToArray();
        if (state.Target is null || !candidates.Contains(state.Target) || --state.RerollIn <= 0)
        {
            PickByThreat(candidates, state);
            return;
        }

        // someone's Threat jumped well above the target's: straight to them
        var top = candidates.MaxBy(x => x.Threat)!;
        if (top == state.Target || top.Threat < (state.Target.Threat * JumpOverTarget) + JumpBesides || top.Threat < (state.TopThreat * JumpOverTop) + JumpBesides) return;

        state.Target = top;
        state.TopThreat = top.Threat;
        state.RerollIn = RerollEvery + _random.Next(RerollJitter);
    }

    // weighted by Threat: the more a fighter has, the likelier it's picked, but nobody with less is ever safe
    private void PickByThreat(BattleFighter[] candidates, BattleAiState state)
    {
        state.RerollIn = RerollEvery + _random.Next(RerollJitter);
        if (_random.NextDouble() < SpicyMood) state.SpicyFor = SpicyUpdates;

        state.TopThreat = candidates.Select(x => x.Threat).DefaultIfEmpty(0).Max();
        state.Target = candidates.Length == 0 ? null : Draw(candidates, x => Math.Max(0, x.Threat) + 1);
    }

    // one of the artes that pass, drawn by weight: turn to face who it's for, then use it
    private void Act(BattleFighter fighter, IBattleActions battle, BattleAiState state)
    {
        if (state.ThinkIn > 0)
        {
            state.ThinkIn--;
            return;
        }

        // a member allowed to use items uses one that's badly needed
        if (pickItem.Pick(fighter, battle) is { } slot)
        {
            battle.UseItem(fighter, slot);
            state.ThinkIn = _random.Next(ThinkMin, ThinkMax);
            return;
        }

        var picks = pickMeIf.Picks(fighter, battle, state.Target);
        if (picks.Count == 0)
        {
            state.ThinkIn = LookAgain;
            return;
        }

        var pick = Draw(picks, x => x.Weight);
        battle.Face(fighter, pick.Facing);
        battle.Use(fighter, pick.Arte);
        state.HelpingFor = 0;
        state.ThinkIn = _random.Next(ThinkMin, ThinkMax);
    }

    private void Move(BattleFighter fighter, IBattleActions battle, BattleAiState state)
    {
        if (Flee(fighter, battle, state)) return;
        if (state.Target is not { } target || target == fighter)
        {
            battleWalk.Stop(fighter);
            battleWalk.Release(fighter);
            return;
        }

        // to its spot beside the target
        if (!battleWalk.ToTarget(fighter, battle, target)) return;

        // there: a little shuffle now and then so it isn't standing dead still in your face
        if (state.WanderFor > 0)
        {
            state.WanderFor--;
            battle.Walk(fighter, state.WanderWay);
        }
        else if (_random.Next(90) == 0)
        {
            state.WanderWay = (Direction) _random.Next(4);
            state.WanderFor = _random.Next(6, 14);
        }
    }

    // the Flee Casting Enemies option: away from the nearest enemy that's casting, and no closer until the cast is done
    private bool Flee(BattleFighter fighter, IBattleActions battle, BattleAiState state)
    {
        var me = battle.Feet(fighter);
        var caster = fighter.Stats.FleeDanger
            ? battle.Fighters.Where(x => x is { IsDown: false, Casting: not null } && battle.IsOpponent(fighter, x)).MinBy(x => Distance(me, battle.Feet(x)))
            : null;
        var distance = caster is null ? double.MaxValue : Distance(me, battle.Feet(caster));
        var flees = distance < battle.Tile * FleeTiles;
        if (flees != state.Fleeing)
        {
            state.Fleeing = flees;
            state.WalkWait = 0;
            battleWalk.Stop(fighter);
        }

        if (distance > battle.Tile * FleeHoldTiles || caster is null) return false;
        if (!flees) return true;

        battleWalk.Away(fighter, battle, battle.Feet(caster), battle.Tile * FleeHoldTiles);
        return true;
    }

    // what sets a temper off, checked every update
    private void Temper(BattleFighter fighter, IBattleActions battle)
    {
        var state = fighter.Ai;
        if (state.SpicyFor > 0) state.SpicyFor--;

        var life = fighter.Stats.Life;
        var lost = state.LastLife is { } before && fighter.Stats.MaxLife > 0 ? Math.Max(0, before - life) / (double) fighter.Stats.MaxLife : 0;
        state.LastLife = life;

        var fallen = battle.Fighters.Count(x => x.IsDown && !battle.IsOpponent(fighter, x));
        var fell = fallen > state.LastFallen;
        state.LastFallen = fallen;

        if ((lost > 0 && _random.NextDouble() < lost * SpicyPerLifeShare) || (fell && _random.NextDouble() < SpicyAllyFell)) state.SpicyFor = SpicyUpdates;
    }

    private T Draw<T>(IReadOnlyList<T> from, Func<T, double> weigh)
    {
        var weights = from.Select(weigh).ToArray();
        var roll = _random.NextDouble() * weights.Sum();
        for (var i = 0; i < from.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0) return from[i];
        }

        return from[^1];
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    #endregion
}
