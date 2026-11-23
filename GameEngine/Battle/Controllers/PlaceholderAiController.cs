using GameEngine.Models.Battle;
using GameLibrary.Enumerations;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// A stand-in until the real AI system: picks a target weighted by Threat, chases it, moves around a little at random
/// once it's close, and swings its first arte (the weapon attack) whenever that would land.
/// Targeting has two speeds (Docs\BattleSystem.md, Threat): it checks every update and switches at once when its target
/// falls, but the weighted pick is only re-rolled every couple of seconds, so it commits to a chase.
/// </summary>
public class PlaceholderAiController : IFighterController
{
    #region Constants

    // updates between re-rolls of the weighted pick (about two seconds, plus a little so fighters don't all switch at once)
    private const int RerollEvery = 120;
    private const int RerollJitter = 40;

    // how close counts as "next to" the target, in tiles, and how long it pauses before and between swings (updates)
    private const double CloseTiles = 1.6;
    private const int ThinkMin = 15;
    private const int ThinkMax = 45;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Methods

    public void Update(BattleFighter fighter, IBattleActions battle)
    {
        var state = fighter.Ai;
        if (state.Target is null or { IsDown: true } || --state.RerollIn <= 0) PickTarget(fighter, battle, state);
        if (state.Target is not { } target) return;

        // a swing, once it has thought about it for a moment
        if (state.ThinkIn > 0) state.ThinkIn--;
        else if (Attack(fighter, battle, target)) state.ThinkIn = _random.Next(ThinkMin, ThinkMax);

        var me = battle.Feet(fighter);
        var them = battle.Feet(target);
        var (dx, dy) = (them.X - me.X, them.Y - me.Y);
        var close = Math.Abs(dx) <= battle.Tile * CloseTiles && Math.Abs(dy) <= battle.Tile * CloseTiles;
        if (!close)
        {
            // the longer way first, so it closes in on a straight-ish line
            battle.Walk(fighter, Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? Direction.Right : Direction.Left) : (dy > 0 ? Direction.Down : Direction.Up));
            return;
        }

        // next to it: a little shuffle now and then so it isn't standing dead still in your face
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

    #endregion

    #region Private Methods

    // weighted by Threat: the more a fighter has, the likelier it's picked, but nobody with less is ever safe
    private void PickTarget(BattleFighter fighter, IBattleActions battle, BattleAiState state)
    {
        state.RerollIn = RerollEvery + _random.Next(RerollJitter);
        var candidates = battle.Fighters.Where(x => !x.IsDown && battle.IsOpponent(fighter, x)).ToArray();
        if (candidates.Length == 0)
        {
            state.Target = null;
            return;
        }

        var weights = candidates.Select(x => Math.Max(0, x.Threat) + 1).ToArray();
        var roll = _random.NextDouble() * weights.Sum();
        for (var i = 0; i < candidates.Length; i++)
        {
            roll -= weights[i];
            if (roll > 0) continue;

            if (state.Target != candidates[i]) state.ThinkIn = _random.Next(ThinkMin, ThinkMax);
            state.Target = candidates[i];
            return;
        }

        state.Target = candidates[^1];
    }

    // the first arte that's ready and would land facing some way: turn that way and use it
    private static bool Attack(BattleFighter fighter, IBattleActions battle, BattleFighter target)
    {
        foreach (var arte in fighter.Slots.OfType<GameLibrary.Models.Artes.Arte>())
        {
            if (!battle.CanUse(fighter, arte)) continue;

            foreach (var facing in new[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up })
            {
                if (!battle.WouldHit(fighter, arte, target, facing)) continue;

                battle.Face(fighter, facing);
                battle.Use(fighter, arte);
                return true;
            }
        }

        return false;
    }

    #endregion
}
