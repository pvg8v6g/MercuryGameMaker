using Windows.Foundation;
using GameEngine.Models.Battle;
using GameLibrary.Enumerations;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Modules;
using GameLibrary.Models.Movements;
using GameLibrary.Models.Actions;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// Which of a fighter's artes it would use right now (Docs\BattleSystem.md, Pick me if). An arte passes only when it's
/// ready, makes sense, and whoever it's for is already close enough, so a passing arte can be used with nothing left to
/// decide. What an arte is for (a heal, a revive, a buff, an attack) is read from the arte itself.
/// </summary>
public class PickMeIf(IGameDataService gameDataService, BattleStates states)
{
    #region Constants

    // Life shares: who needs a heal, who needs it whatever the arte's Targets Wanted, and who's nearly down
    private const double HealBelow = 0.7;
    private const double EmergencyBelow = 0.35;

    // a heal over time is started earlier, and never on someone who'd fall before it helps
    private const double SlowHealBelow = 0.85;
    private const double SlowHealAbove = 0.3;

    // an arte that restores Mana wants someone under this share of theirs
    private const double ManaBelow = 0.5;

    // the other side's combined Life: the opening of a fight is above the first, late in it is below the second
    private const double OpeningAbove = 0.7;
    private const double LateBelow = 0.5;

    // a buff waits until an opponent is this close, so it isn't spent walking across the field
    private const double BuffTiles = 4.0;

    // a projectile is for the target, but goes at anyone standing this close too (in tiles)
    private const double InTheFaceTiles = 2.0;

    // how likely each kind is drawn against an attack's 1
    private const double ReviveWeight = 8.0;
    private const double HealWeight = 6.0;
    private const double CureWeight = 3.0;
    private const double OpeningBuffWeight = 3.0;
    private const double TauntWeight = 4.0;
    private const double ThreatDropWeight = 4.0;
    private const double HealerFactor = 2.0;

    #endregion

    #region Methods

    // every arte that passes right now
    public List<ArtePick> Picks(BattleFighter fighter, IBattleActions battle, BattleFighter? target)
    {
        var picks = new List<ArtePick>();
        foreach (var arte in fighter.Slots.OfType<Arte>())
        {
            if (arte.ArteUse == ItemUse.MenuOnly || !battle.CanUse(fighter, arte)) continue;

            var kind = Kind(arte);
            if (!CanAfford(fighter, arte, kind) || Facing(fighter, battle, arte, kind, target) is not { } facing) continue;

            picks.Add(new ArtePick(arte, facing, Weight(fighter, battle, kind)));
        }

        return picks;
    }

    // an actor with the Healer battle option, or an enemy with Healer ticked: it goes to whoever needs it. Anyone else with a
    // heal still uses it on an ally in its reach, it just doesn't walk off to find one
    public bool IsHealer(BattleFighter fighter) => fighter.Stats.Healer;

    // the ally a healer should walk to: one a ready revive, heal or cure is needed for, the worst off first, and early in
    // the fight whoever is nearest for a buff that's shared. Null when nobody needs it
    public BattleFighter? Needy(BattleFighter fighter, IBattleActions battle)
    {
        var allies = battle.Fighters.Where(x => x != fighter && !battle.IsOpponent(fighter, x)).ToArray();
        var ready = fighter.Slots.OfType<Arte>()
            .Where(x => x.EffectScope is not EffectScope.Self and not EffectScope.Party)
            .Where(x => !fighter.Cooldowns.ContainsKey(x.Guid) && fighter.Stats.Mana >= x.Cost && !states.BlocksArtes(fighter))
            .Select(x => (Arte: x, Kind: Kind(x)))
            .ToArray();

        foreach (var kind in new[] { ArteKind.Revive, ArteKind.Heal, ArteKind.SlowHeal, ArteKind.Cure })
        {
            var artes = ready.Where(x => x.Kind == kind).Select(x => x.Arte).ToArray();
            var needy = allies.Where(x => artes.Any(arte => Needs(arte, kind, x))).OrderBy(LifeShare).FirstOrDefault();
            if (needy is not null) return needy;
        }

        // in the opening, once the fight is nearly on, the nearest ally without the buff
        if (SideLife(fighter, battle) < OpeningAbove || !OpponentWithin(fighter, battle, BuffTiles)) return null;
        var buffs = ready.Where(x => x.Kind == ArteKind.Buff).Select(x => x.Arte).ToArray();
        return allies.Where(x => buffs.Any(arte => Needs(arte, ArteKind.Buff, x))).OrderBy(x => Distance(battle.Feet(fighter), battle.Feet(x))).FirstOrDefault();
    }

    #endregion

    #region Private Methods

    // the way to face so it lands on the right fighters, null when it wouldn't
    private Direction? Facing(BattleFighter fighter, IBattleActions battle, Arte arte, ArteKind kind, BattleFighter? target)
    {
        if (!RightTime(fighter, battle, arte)) return null;

        foreach (var focus in Focuses(fighter, battle, arte, kind, target))
        {
            foreach (var facing in Facings(fighter, battle, focus))
            {
                if (!battle.WouldHit(fighter, arte, focus, facing)) continue;

                var hits = battle.Fighters.Where(x => battle.WouldHit(fighter, arte, x, facing)).ToArray();
                if (Enough(fighter, battle, arte, kind, hits)) return facing;
            }
        }

        return null;
    }

    // who the arte could be turned toward, the best first. An attack is for the target first, then for anyone else it would
    // land on (Threat says where a fighter walks, not that it won't hit whoever is in its way). A projectile at someone
    // other than the target is only for someone in its face, or Threat would mean nothing to a fighter at range
    private IEnumerable<BattleFighter> Focuses(BattleFighter fighter, IBattleActions battle, Arte arte, ArteKind kind, BattleFighter? target)
    {
        var me = battle.Feet(fighter);
        if (kind == ArteKind.ThreatDrop || (kind == ArteKind.Buff && arte.Threat > 0)) return [fighter];
        if (kind is not ArteKind.Attack and not ArteKind.Taunt) return battle.Fighters.Where(x => !battle.IsOpponent(fighter, x) && Needs(arte, kind, x)).OrderBy(LifeShare);

        return battle.Fighters
            .Where(x => !x.IsDown && battle.IsOpponent(fighter, x) && !states.CantBeTargeted(x))
            .Where(x => x == target || arte.Projectiles.Count == 0 || Distance(me, battle.Feet(x)) <= battle.Tile * InTheFaceTiles)
            .OrderBy(x => x == target ? 0 : 1)
            .ThenBy(x => Distance(me, battle.Feet(x)));
    }

    // enough of the right fighters would be hit. Targets Wanted never asks for more than are still standing
    private bool Enough(BattleFighter fighter, IBattleActions battle, Arte arte, ArteKind kind, BattleFighter[] hits)
    {
        var friends = hits.Where(x => !battle.IsOpponent(fighter, x)).ToArray();
        var standing = battle.Fighters.Where(x => !x.IsDown && !battle.IsOpponent(fighter, x)).ToArray();
        switch (kind)
        {
            case ArteKind.Revive:
                return friends.Any(x => x.IsDown);
            case ArteKind.Heal:
                return friends.Any(x => !x.IsDown && LifeShare(x) < EmergencyBelow) || friends.Count(x => Needs(arte, kind, x)) >= Math.Min(arte.TargetsWanted, standing.Length);
            case ArteKind.SlowHeal:
                return friends.Count(x => Needs(arte, kind, x)) >= Math.Min(arte.TargetsWanted, standing.Length);
            case ArteKind.Cure:
            case ArteKind.Mana:
                return friends.Any(x => Needs(arte, kind, x));
            case ArteKind.ThreatDrop:
                return WantsThreatDrop(fighter, battle);
            case ArteKind.Buff when arte.Threat > 0:
                // a tank's: when the others have more Threat, or it would add something
                var draws = LifeShare(fighter) >= EmergencyBelow && !HasMostThreat(fighter, battle);
                return OpponentWithin(fighter, battle, BuffTiles) && (draws || friends.Any(x => Needs(arte, kind, x)));
            case ArteKind.Buff:
                // a shared buff wants someone to share it with, when there's anyone
                var wanted = arte.EffectScope is EffectScope.Self or EffectScope.Party ? 1 : Math.Max(2, arte.TargetsWanted);
                return OpponentWithin(fighter, battle, BuffTiles) && friends.Count(x => Needs(arte, kind, x)) >= Math.Min(wanted, standing.Length);
            case ArteKind.Attack:
            case ArteKind.Taunt:
                break;
            default:
                return false;
        }

        var opponents = battle.Fighters.Where(x => !x.IsDown && battle.IsOpponent(fighter, x)).ToArray();
        var hit = opponents.Where(hits.Contains).ToArray();
        if (hit.Length == 0 || hit.Length < Math.Min(arte.TargetsWanted, opponents.Length)) return false;

        // a taunt is for someone going after an ally. An enemy's works on anyone (it cuts what they do to the others)
        if (kind == ArteKind.Taunt) return hit.Any(x => states.TauntedBy(x) is null && (fighter.IsEnemy || x.Ai.Target != fighter));
        if (arte.AiTiming == ArteTiming.LowLifeTarget && !hit.Any(x => LifeShare(x) < EmergencyBelow)) return false;

        // an arte that hits everyone isn't worth it when it would catch more friends than opponents
        if (friends.Count(x => !x.IsDown && x != fighter) > hit.Length) return false;

        // one that only puts states on has to have someone to put them on
        return arte.HpDamage > 0 || arte.ManaDamage > 0 || arte.AddStates.Count == 0 || hit.Any(x => arte.AddStates.Any(id => !states.Has(x, id)));
    }

    // everyone drops it when they have the most. A tank wants the most, so only when it thinks its life is in danger. With
    // no one else standing there's nobody to hand it to
    private static bool WantsThreatDrop(BattleFighter fighter, IBattleActions battle)
    {
        if (!battle.Fighters.Any(x => x != fighter && !x.IsDown && !battle.IsOpponent(fighter, x))) return false;
        if (fighter.Threat <= 0 || !HasMostThreat(fighter, battle)) return false;

        return !fighter.Slots.OfType<Arte>().Any(x => x.Threat > 0) || LifeShare(fighter) < EmergencyBelow;
    }

    private static bool HasMostThreat(BattleFighter fighter, IBattleActions battle)
    {
        return battle.Fighters.Where(x => x != fighter && !x.IsDown && !battle.IsOpponent(fighter, x)).All(x => x.Threat < fighter.Threat);
    }

    // whether the arte would do this ally any good
    private bool Needs(Arte arte, ArteKind kind, BattleFighter ally)
    {
        if (kind == ArteKind.Revive) return ally.IsDown;
        if (ally.IsDown) return false;

        return kind switch
        {
            ArteKind.Heal => LifeShare(ally) < HealBelow,
            ArteKind.SlowHeal => LifeShare(ally) is < SlowHealBelow and >= SlowHealAbove && (arte.AddStates.Count == 0 || arte.AddStates.Any(x => !states.Has(ally, x))),
            ArteKind.Cure => ally.States.Any(x => (arte.RemoveNegativeStates && !x.State.Friendly) || arte.RemoveStates.Contains(x.State.Guid)),
            ArteKind.Buff => arte.AddStates.Any(x => !states.Has(ally, x)),
            ArteKind.Mana => ally.Stats.MaxMana > 0 && ally.Stats.Mana < ally.Stats.MaxMana * ManaBelow,
            _ => false
        };
    }

    private static bool RightTime(BattleFighter fighter, IBattleActions battle, Arte arte) => arte.AiTiming switch
    {
        ArteTiming.Opening => SideLife(fighter, battle) >= OpeningAbove,
        ArteTiming.Late => SideLife(fighter, battle) < LateBelow,
        _ => true
    };

    // a risky arte takes a temper. A healer keeps enough Mana back for its cheapest heal, unless it's in a temper too
    private bool CanAfford(BattleFighter fighter, Arte arte, ArteKind kind)
    {
        if (fighter.Ai.SpicyFor > 0) return true;
        if (arte.Risky) return false;
        if (arte.Cost <= 0 || kind is not ArteKind.Attack and not ArteKind.Buff || !IsHealer(fighter)) return true;

        var heals = fighter.Slots.OfType<Arte>().Where(Heals).Select(x => x.Cost).ToArray();
        return heals.Length == 0 || fighter.Stats.Mana - arte.Cost >= heals.Min();
    }

    private double Weight(BattleFighter fighter, IBattleActions battle, ArteKind kind)
    {
        var weight = kind switch
        {
            ArteKind.Revive => ReviveWeight,
            ArteKind.Heal or ArteKind.SlowHeal => HealWeight,
            ArteKind.Cure or ArteKind.Mana => CureWeight,
            ArteKind.Taunt => TauntWeight,
            ArteKind.ThreatDrop => ThreatDropWeight,
            ArteKind.Buff when SideLife(fighter, battle) >= OpeningAbove => OpeningBuffWeight,
            _ => 1.0
        };
        return kind is not ArteKind.Attack and not ArteKind.Taunt && IsHealer(fighter) ? weight * HealerFactor : weight;
    }

    private ArteKind Kind(Arte arte)
    {
        if (arte.RemoveStates.Contains(states.IncapacitatedId)) return ArteKind.Revive;
        if (arte.EffectScope is not (EffectScope.Allies or EffectScope.Self or EffectScope.Party or EffectScope.All or EffectScope.AllNotDead)) return arte.HpDamage <= 0 && states.Taunts(arte) ? ArteKind.Taunt : ArteKind.Attack;
        if (arte.Threat < 0) return ArteKind.ThreatDrop;
        if (HealsOverTime(arte)) return ArteKind.SlowHeal;
        if (arte.HpDamage < 0) return ArteKind.Heal;
        if (arte.EffectScope is EffectScope.All or EffectScope.AllNotDead) return ArteKind.Attack;
        if (arte.AddStates.Count > 0 || arte.Threat > 0) return ArteKind.Buff;
        if (arte.RemoveNegativeStates || arte.RemoveStates.Count > 0) return ArteKind.Cure;
        return arte.ManaDamage < 0 ? ArteKind.Mana : ArteKind.Nothing;
    }

    private bool Heals(Arte arte) => Kind(arte) is ArteKind.Heal or ArteKind.SlowHeal;

    // a state that ticks healing, or projectiles that pulse healing artes along their route (healing ground)
    private bool HealsOverTime(Arte arte)
    {
        var ticks = arte.AddStates.Select(id => gameDataService.States.FirstOrDefault(x => x.Guid == id)?.TickArte);
        var pulses = arte.Projectiles.SelectMany(x => x.Pages).SelectMany(x => x.Actions).OfType<EntityMovementAction>()
            .SelectMany(x => x.Route).OfType<ActivateArteMovement>().Select(x => (Guid?) x.ArteId);
        var artes = ticks.Concat(pulses).OfType<Guid>().Select(id => gameDataService.Artes.FirstOrDefault(x => x.Guid == id));
        return arte.HpDamage >= 0 && artes.Any(x => x is { HpDamage: < 0 });
    }

    // the way the target is farther, then the other way when it's off to that side as well (a diagonal). On itself it stays
    // the way it faces
    private static Direction[] Facings(BattleFighter fighter, IBattleActions battle, BattleFighter target)
    {
        var current = fighter.Body.GetModule<PositionModule>() is { } position ? (Direction) (Math.Max(0, (int) position.Direction) % 4) : Direction.Down;
        var me = battle.Feet(fighter);
        var them = battle.Feet(target);
        var (dx, dy) = (them.X - me.X, them.Y - me.Y);
        if (target == fighter || (dx == 0 && dy == 0)) return [current];

        var horizontal = dx > 0 ? Direction.Right : Direction.Left;
        var vertical = dy > 0 ? Direction.Down : Direction.Up;
        var sideways = Math.Abs(dx) > Math.Abs(dy);
        var (first, second) = sideways ? (horizontal, vertical) : (vertical, horizontal);
        return Math.Min(Math.Abs(dx), Math.Abs(dy)) > battle.Tile / 2.0 ? [first, second] : [first];
    }

    private static bool OpponentWithin(BattleFighter fighter, IBattleActions battle, double tiles)
    {
        var me = battle.Feet(fighter);
        return battle.Fighters.Any(x => !x.IsDown && battle.IsOpponent(fighter, x) && Distance(me, battle.Feet(x)) <= battle.Tile * tiles);
    }

    // the share of its Life the other side has left, the fallen included
    private static double SideLife(BattleFighter fighter, IBattleActions battle)
    {
        var opponents = battle.Fighters.Where(x => battle.IsOpponent(fighter, x)).ToArray();
        var maximum = opponents.Sum(x => (double) x.Stats.MaxLife);
        return maximum <= 0 ? 0 : opponents.Sum(x => (double) x.Stats.Life) / maximum;
    }

    private static double LifeShare(BattleFighter fighter) => fighter.Stats.MaxLife <= 0 ? 1 : (double) fighter.Stats.Life / fighter.Stats.MaxLife;

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    #endregion

    #region Types

    private enum ArteKind
    {
        Nothing,
        Attack,
        Revive,
        Heal,
        SlowHeal,
        Cure,
        Buff,
        Mana,
        Taunt,
        ThreatDrop
    }

    #endregion
}
