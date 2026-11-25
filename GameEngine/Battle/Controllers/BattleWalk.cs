using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Navigation;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// How the AI walks in battle. A fighter claims a spot beside its target (one of the four sides, or a corner when those
/// are taken) that no ally is claiming, so a group spreads out around someone, and walks there on a path that goes around
/// the others. Paths turn as little as they can and are kept for a while, so a diagonal is an L and never a left, up,
/// left, up flicker. A fighter with Flanks would rather have the spot behind its target.
/// </summary>
public class BattleWalk(NavigationService navigationService, PathWalker pathWalker, HitboxSystem hitboxSystem)
{
    #region Constants

    // what a turn costs a path, and walking through someone who's on the move, in path cells (a quarter tile each)
    private const double TurnCost = 2;
    private const double MovingCost = 6;

    // updates a path is kept before it follows a target that moved a tile, and before it's searched again anyway
    private const int KeepPath = 30;
    private const int OldPath = 90;

    // updates a blocked fighter stands still, plus a random few
    private const int BlockedWait = 10;

    // updates before a claim on a spot is thought over, and sooner for a corner (a narrow swing can't land from one)
    private const int KeepClaim = 180;
    private const int KeepCorner = 120;

    // in tiles: what makes a spot less (or more) wanted, and how far past its spot a target gets before it's followed
    private const double CornerPenalty = 1.5;
    private const double FlankBack = -3;
    private const double FlankFront = 2;
    private const double SpotJitter = 0.25;
    private const double FollowTiles = 0.6;

    // the gap left between two bodies, in character steps
    private const int GapSteps = 2;

    // below, left, right and above the target, then the corners. The first four are in Direction's order
    private static readonly (int X, int Y)[] Spots = [(0, 1), (-1, 0), (1, 0), (0, -1), (-1, 1), (1, 1), (-1, -1), (1, -1)];

    #endregion

    #region Methods

    // one update of walking to a spot beside the target. True once it's there
    public bool ToTarget(BattleFighter fighter, IBattleActions battle, BattleFighter target)
    {
        var state = fighter.Ai;
        if (hitboxSystem.Main(fighter.Body) is not { } me || hitboxSystem.Main(target.Body) is not { } them) return false;

        state.ClaimAge++;
        var (reachX, reachY) = Reach(me, them);
        if (state.AtSpot)
        {
            var (dx, dy) = (Center(them).X - Center(me).X, Center(them).Y - Center(me).Y);
            var follow = battle.Tile * FollowTiles;
            if (Math.Abs(dx) > reachX + follow || Math.Abs(dy) > reachY + follow) state.ClaimTarget = null;
        }

        if (state.ClaimTarget != target || IsOld(fighter, state)) Claim(fighter, battle, target, me, them);
        if (state.ClaimTarget is null)
        {
            Stop(fighter);
            return false;
        }

        if (state.AtSpot) return true;

        var spot = Spot(state.ClaimSpot, me, them);
        var tolerance = navigationService.CellPixels;
        if (Math.Abs(spot.X - Center(me).X) <= tolerance && Math.Abs(spot.Y - Center(me).Y) <= tolerance && !fighter.Body.State<MovementState>().IsMoving)
        {
            state.AtSpot = true;
            Stop(fighter);
            return true;
        }

        Follow(fighter, spot, battle.Tile, new CenterGoal(spot.X, spot.Y, tolerance));
        return false;
    }

    // one update of walking until it's a distance from a point (scaled map pixels)
    public void Away(BattleFighter fighter, IBattleActions battle, Point from, double distance)
    {
        Release(fighter);
        Follow(fighter, from, battle.Tile, new AwayGoal(from.X, from.Y, distance));
    }

    // drops the path it's on, and the spot it claimed with it
    public void Release(BattleFighter fighter)
    {
        fighter.Ai.ClaimTarget = null;
        fighter.Ai.AtSpot = false;
    }

    public void Stop(BattleFighter fighter)
    {
        fighter.Ai.Path = null;
        fighter.Ai.Walk.Reset();
    }

    #endregion

    #region Private Methods

    private static bool IsOld(BattleFighter fighter, BattleAiState state)
    {
        if (state.ClaimSpot >= 4 && state.ClaimAge > KeepCorner) return true;
        return state.ClaimAge > KeepClaim && (!state.AtSpot || fighter.Stats.Flanks);
    }

    // the nearest spot nobody on its side has claimed and nothing stands on. None: it waits for one
    private void Claim(BattleFighter fighter, IBattleActions battle, BattleFighter target, MapHitbox me, MapHitbox them)
    {
        var state = fighter.Ai;
        var wasSpot = state.ClaimTarget == target ? state.ClaimSpot : -1;
        state.ClaimTarget = null;
        state.ClaimAge = 0;

        var best = double.MaxValue;
        for (var i = 0; i < Spots.Length; i++)
        {
            if (battle.Fighters.Any(x => x != fighter && !x.IsDown && x.Ai.ClaimTarget == target && x.Ai.ClaimSpot == i)) continue;

            var spot = Spot(i, me, them);
            if (!IsOpen(fighter, battle, target, me, spot)) continue;

            var cost = Cost(fighter, battle, target, me, spot, i);
            if (cost >= best) continue;

            best = cost;
            state.ClaimTarget = target;
            state.ClaimSpot = i;
        }

        if (state.ClaimTarget is not null && state.ClaimSpot == wasSpot) return;

        state.AtSpot = false;
        Stop(fighter);
    }

    private static double Cost(BattleFighter fighter, IBattleActions battle, BattleFighter target, MapHitbox me, Point spot, int index)
    {
        var center = Center(me);
        var tiles = (Math.Abs(spot.X - center.X) + Math.Abs(spot.Y - center.Y)) / battle.Tile;
        tiles += Random.Shared.NextDouble() * SpotJitter;
        if (index >= 4) tiles += CornerPenalty;
        if (!fighter.Stats.Flanks || !battle.IsOpponent(fighter, target)) return tiles;

        var facing = target.Body.GetModule<PositionModule>() is { } position ? Math.Max(0, (int) position.Direction) % 4 : 0;
        if (index == facing) return tiles + FlankFront;

        // the sides are numbered like the directions, so opposite ones add up to 3
        return index == 3 - facing ? tiles + FlankBack : tiles;
    }

    // no wall or prop there, and nobody standing on it
    private bool IsOpen(BattleFighter fighter, IBattleActions battle, BattleFighter target, MapHitbox me, Point spot)
    {
        if (fighter.Body.GetModule<PositionModule>() is not { } position || fighter.Body.GetModule<EntityHitboxModule>() is not { } module) return false;

        var center = Center(me);
        var there = hitboxSystem.Project(module, position, spot.X - center.X, spot.Y - center.Y);
        if (hitboxSystem.HitsCollisionArea([], there, Direction.Down, HitboxSystem.Level(position))) return false;

        return !battle.Fighters.Any(x =>
            x != fighter && x != target && !x.IsDown &&
            !x.Body.State<MovementState>().IsMoving &&
            hitboxSystem.Main(x.Body) is { } body && there.Any(body.Collide));
    }

    // along a path, a cell at a time. The path is kept until it's old or where it's going has moved about a tile
    private void Follow(BattleFighter fighter, Point where, double tile, NavGoal goal)
    {
        var state = fighter.Ai;
        var body = fighter.Body;
        if (body.State<BattleLockState>().CannotMove)
        {
            Stop(fighter);
            return;
        }

        if (state.WalkWait > 0)
        {
            state.WalkWait--;
            return;
        }

        state.PathAge++;
        if (body.State<MovementState>().IsMoving || body.GetModule<PositionModule>() is not { } position) return;

        if (state.Walk.Started && !Step(fighter, position, state.Walk.Way)) return;

        var done = state.Path is null || state.PathIndex >= state.Path.Count;
        var moved = Math.Abs(where.X - state.PathTo.X) + Math.Abs(where.Y - state.PathTo.Y) > tile;
        if (done || state.PathAge >= OldPath || (moved && state.PathAge >= KeepPath))
        {
            var found = navigationService.FindPath(body, goal, state.LastWay, TurnCost, MovingCost);
            if (found is not null)
            {
                state.Path = found.Cells;
                state.PathIndex = 0;
                state.PathAge = 0;
                state.PathTo = where;
                if (found.Cells.Count == 0)
                {
                    // there, or nowhere closer to get to: think again in a moment
                    state.WalkWait = BlockedWait + Random.Shared.Next(BlockedWait);
                    if (!found.Reached) Release(fighter);
                    return;
                }
            }
            else if (done)
            {
                // this update's searches are used up, it asks again on the next
                return;
            }
        }

        if (state.Path is { } path && state.PathIndex < path.Count) Step(fighter, position, path[state.PathIndex]);
    }

    // true when the path cell was finished. Someone in the way: it waits a moment and looks for another way
    private bool Step(BattleFighter fighter, PositionModule position, Direction way)
    {
        var state = fighter.Ai;
        state.LastWay = way;
        var result = pathWalker.Walk(fighter.Body, position, state.Walk, way, navigationService.CellPixels);
        switch (result)
        {
            case WalkResult.Done:
                state.PathIndex++;
                return true;
            case WalkResult.Blocked:
                Stop(fighter);
                Release(fighter);
                state.WalkWait = BlockedWait + Random.Shared.Next(BlockedWait);
                return false;
            default:
                return false;
        }
    }

    private Point Spot(int index, MapHitbox me, MapHitbox them)
    {
        var (reachX, reachY) = Reach(me, them);
        var center = Center(them);
        return new Point(center.X + (Spots[index].X * reachX), center.Y + (Spots[index].Y * reachY));
    }

    // from middle to middle when the two stand side by side
    private (double X, double Y) Reach(MapHitbox me, MapHitbox them)
    {
        var gap = navigationService.StepPixels * GapSteps;
        return (((me.Width + them.Width) / 2.0) + gap, ((me.Height + them.Height) / 2.0) + gap);
    }

    private static Point Center(MapHitbox box) => new(box.X + (box.Width / 2.0), box.Y + (box.Height / 2.0));

    #endregion
}
