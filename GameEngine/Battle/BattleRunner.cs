using System.Collections.Concurrent;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using GameEngine.Battle.Controllers;
using GameEngine.Interface;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Party;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Items;
using GameLibrary.Models.Modules;
using GameLibrary.Models.Movements;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameEngine.Battle;

/// <summary>
/// Runs the fight once an update while a battle is on and nothing pauses it. Each fighter's controller decides what it
/// does (the player for the leader, the AI for everyone else) through the actions here, so the rules are the same for
/// everyone: casting (a cast locks the caster in place and can't be cancelled), then either detonating at the user (the
/// arte's hitboxes laid out from the user's feet tile, turned the way they face) or firing the arte's projectiles, every
/// hit worked out by the hit calculator, Life, Threat, falling, and victory or defeat.
/// A projectile starts on the user's feet tile facing the way they face and walks its page's movement routes in order.
/// Its arte lands where it detonates, owned by the user with the numbers they had when it was fired, and it's removed.
/// </summary>
public class BattleRunner(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    HitboxSystem hitboxSystem,
    AnimationSystem animationSystem,
    MovementRouteSystem movementRouteSystem,
    BattleAttributes attributes,
    BattleStates states,
    BattleStateTester stateTester,
    PartyStates partyStates,
    HitCalculator hitCalculator,
    MovementSystem movementSystem,
    PlayerController playerController,
    AiController aiController,
    BattleHud battleHud,
    DebugOverlay debugOverlay,
    IJsonService jsonService,
    CharacterImageSystem characterImageSystem,
    PlayerSpawner playerSpawner,
    PartyService partyService,
    BattleDrops drops,
    VictoryScreen victoryScreen,
    GameOverScreen gameOverScreen,
    GameOverSequence gameOverSequence,
    MapAudio mapAudio) : IBattleActions
{
    #region Constants

    // the tiny cooldown after any arte, only so one key press can't fire two
    private const int GlobalCooldown = 8;

    // the pause between the last enemy falling and the battle ending
    private const int EndDelay = 60;

    // the same pause when drops are lying on the map: about 5 seconds to pick them up, and what's left is lost
    private const int PickupDelay = 300;

    // running away: the key is held this many updates (3 seconds) to fill the bar, and then it can still fail. Tuning values
    private const int FleeUpdates = 180;
    private const double FleeFailChance = 0.1;

    // the one cooldown every item in the party shares: 15 seconds. A tuning value
    private const int ItemCooldownUpdates = 900;

    // what's left of a taunted party member's damage to anyone but the taunter. A tuning value
    private const double TauntedDamage = 0.3;

    // how long the battle music takes to fade out once the last enemy has fallen
    private const double MusicFadeSeconds = 6.0;

    #endregion

    #region Fields

    private ActiveBattle? _battle;
    private int _endTimer;

    // how long the pause after the last enemy falls lasts: the full pickup time when anything dropped, whether or not it's
    // all taken sooner, otherwise the short pause
    private int _endWait = EndDelay;

    // the last enemy has fallen and the rewards are handed out
    private bool _won;

    // the victory screen is up (or was), and the battle ends once it's closed
    private bool _summarized;

    // how long the party has held the flee key: it stops fighting while the bar fills, and the enemies don't
    private int _fleeProgress;

    // every party member on the field is down and the enemies aren't: the party lost, and that's final (a win can't follow)
    private bool _lost;

    // the game over screen is up (or was), and the defeat is settled once it's closed
    private bool _gameOver;

    // how the fight began, so a retry can put it all back: the party's Life and Mana, the money and the bag
    private readonly List<(BattleFighter fighter, int life, int mana)> _startStats = [];
    private int _startMoney;
    private readonly Dictionary<Guid, int> _startItems = [];
    private Dictionary<Guid, List<CarriedState>> _startStates = [];

    // what the win handed out, listed on the victory screen
    private readonly BattleRewards _rewards = new();

    private readonly List<BattleProjectile> _projectiles = [];

    // a projectile image's top left when its feet stand on tile 0, 0 (unscaled), measured when the battle begins
    private readonly ConcurrentDictionary<string, Point> _standOffsets = [];

    #endregion

    #region Methods

    // called from the scene once an update while the battle map is up and the game isn't paused
    public void Update()
    {
        if (gameStateService.Battle is not { IsEnding: false, Fighters.Count: > 0 } battle) return;
        if (battle != _battle) Begin(battle);

        // TEST: F9 ends the battle straight away
        if (inputService.IsDown(VirtualKey.F9))
        {
            End(battle);
            return;
        }

        // the victory screen has the party's attention: the fight is over, and it ends the moment the screen closes
        if (_summarized)
        {
            if (!victoryScreen.IsActive) End(battle);
            return;
        }

        // the game over screen has the party's attention: the defeat is settled the moment it's closed
        if (_gameOver)
        {
            if (!gameOverScreen.IsActive) Settle(battle);
            return;
        }

        // TEST: F5 to F8 put states on the fighters
        stateTester.Update(battle);

        UpdateFlee(battle);
        if (battle.IsEnding) return;

        UpdateProjectiles();
        if (battle.ItemCooldown > 0) battle.ItemCooldown--;
        foreach (var fighter in battle.Fighters) Tick(fighter);

        // a party that's running away doesn't fight: nobody on its side decides anything until the key is let go
        var fleeing = _fleeProgress > 0;
        foreach (var fighter in battle.Fighters.Where(x => !x.IsDown && !(fleeing && !x.IsEnemy))) fighter.Controller?.Update(fighter, this);

        Collect(battle);
        CheckForEnd(battle);
    }

    #endregion

    #region Battle Actions

    public IReadOnlyList<BattleFighter> Fighters => _battle?.Fighters ?? [];

    public double Tile => gameDataService.GridSize * gameDataService.ScaleFactor;

    public bool IsOpponent(BattleFighter fighter, BattleFighter target) => target.OriginalTeam != fighter.Team;

    public bool CanUse(BattleFighter fighter, Arte arte)
    {
        return !fighter.IsDown && !(_fleeProgress > 0 && !fighter.IsEnemy) && fighter.Casting is null && fighter.GlobalCooldown <= 0 && !fighter.Cooldowns.ContainsKey(arte.Guid) && fighter.Stats.Mana >= arte.Cost && !states.BlocksArtes(fighter);
    }

    // an item is used where the fighter stands: it costs nothing but the item itself (it leaves the bag) and starts the
    // party's shared item cooldown. No cast, no lock. Only a free fighter can use one: not down, running, casting or stunned
    public void UseItem(BattleFighter fighter, int slot)
    {
        if (_battle is not { } battle || !CanUseItem(fighter, slot) || battle.ItemSlots[slot] is not { } item) return;

        // an item that's already gone (spent, or sold from a page mid fight) empties its slot and does nothing
        if (gameStateService.Items.GetValueOrDefault(item.Guid) <= 0)
        {
            ClearItem(battle, item);
            return;
        }

        partyService.ChangeItem(item.Guid, -1);
        if (gameStateService.Items.GetValueOrDefault(item.Guid) <= 0) ClearItem(battle, item);

        battle.ItemCooldown = battle.ItemCooldownTotal = ItemCooldownUpdates;
        animationSystem.Play(fighter.Body, item.CasterAnimation);
        LandItem(item, fighter);
    }

    public IReadOnlyList<Consumable?> ItemSlots => _battle?.ItemSlots ?? [];

    public bool CanUseItem(BattleFighter fighter, int slot)
    {
        if (_battle is not { } battle || fighter.IsEnemy || _won || _lost || battle.ItemSlots.ElementAtOrDefault(slot) is null) return false;
        return !fighter.IsDown && _fleeProgress <= 0 && fighter.Casting is null && !states.BlocksItems(fighter) && battle.ItemCooldown <= 0;
    }

    public bool ItemWouldHit(BattleFighter fighter, Consumable item, BattleFighter target)
    {
        var revives = item.ClearAllStates || item.RemoveStates.Contains(Incapacitated());
        return InScope(item.EffectScope, fighter, fighter.Team, target, revives) && Touches(target, ItemBoxes(item, fighter), item.EffectScope);
    }

    // an arte with projectiles is judged by a straight line as far as its projectiles step forward. Both that and an area
    // leave some give (the target can walk while it's on the way), so this is whether it's worth using, not a hit check
    public bool WouldHit(BattleFighter fighter, Arte arte, BattleFighter target, Direction facing)
    {
        var revives = arte.RemoveStates.Contains(Incapacitated());
        if (!InScope(arte.EffectScope, fighter, fighter.Team, target, revives)) return false;

        // projectiles placed at random could land anywhere, so any target in scope will do
        if (arte.Projectiles.Any(x => Routes(x).Any(route => route.Route.OfType<PlaceRandomlyMovement>().Any()))) return true;
        if (arte.Projectiles.Count > 0) return InFlightLine(fighter, arte, target, facing);

        Rect[] boxes = [.. Hitboxes(fighter.Body, arte, facing)];
        return Touches(target, boxes, arte.EffectScope) && (arte.DirectionRotate || WellInside(fighter, target, boxes, arte.EffectScope));
    }

    public void Walk(BattleFighter fighter, Direction direction)
    {
        if (fighter.IsDown || fighter.Body.State<BattleLockState>().CannotMove) return;

        movementSystem.Move(fighter.Body, direction);
    }

    // the sheet row picks the character, only the way it faces changes
    public void Face(BattleFighter fighter, Direction direction)
    {
        if (fighter.Body.State<BattleLockState>().Stunned) return;
        if (fighter.Body.GetModule<PositionModule>() is not { } position) return;

        position.Direction = (Direction) ((Math.Max(0, (int) position.Direction) / 4 * 4) + (int) direction);
    }

    public Point Feet(BattleFighter fighter)
    {
        var tile = FeetTile(fighter.Body);
        return new Point(tile.X + (Tile / 2.0), tile.Y + (Tile / 2.0));
    }

    #endregion

    #region Projectiles

    // every projectile of the arte leaves at once from the origin's feet tile, facing the way the origin faces. Each is
    // a fresh copy of the arte's projectile on the battle map, walking only its page's own movement routes: the page's
    // actions are taken off the copy, so the page runner never restarts them
    private void Fire(Arte arte, AttackerSnapshot attacker, MapEntity origin, Direction facing, int depth)
    {
        var scale = gameDataService.ScaleFactor;
        var feet = FeetTile(origin);
        var level = origin.GetModule<PositionModule>()?.Z ?? 0;
        foreach (var template in arte.Projectiles)
        {
            if (jsonService.Copy(template) is not { Pages.Count: > 0 } entity) continue;

            entity.Guid = Guid.NewGuid();
            var page = entity.Pages.OrderByDescending(x => x.Id).FirstOrDefault(x => x.Conditions.Count == 0) ?? entity.Pages.OrderByDescending(x => x.Id).First();
            var routes = page.Actions.OfType<EntityMovementAction>().Where(x => x.EntityId is null).ToArray();
            foreach (var each in entity.Pages)
            {
                each.Actions.Clear();
                each.RemembersPosition = false;
                if (each.Modules.OfType<PositionModule>().FirstOrDefault() is not { } position) continue;

                // the picture is lifted so its feet stand on the origin's feet tile, like a character on its tile
                var image = each.Modules.OfType<CharacterImageModule>().FirstOrDefault()?.ImageName;
                var offset = image is not null && _standOffsets.TryGetValue(image, out var known) ? known : default;
                position.X = feet.X + (offset.X * scale);
                position.Y = feet.Y + (offset.Y * scale);
                position.Z = level;
                position.Direction = (Direction) ((Math.Max(0, (int) position.Direction) / 4 * 4) + (int) facing);
            }

            var body = new MapEntity(entity) { ActivePage = page };
            body.State<ProjectileState>().IsProjectile = true;
            gameStateService.MapEntities.Add(body);
            _projectiles.Add(new BattleProjectile(body, arte, attacker, routes, entity.DetonateOnContact, entity.DetonateAtRouteEnd, depth));
        }
    }

    // once an update, after the systems moved everyone: what the routes asked for, contact, walls, and the next route
    private void UpdateProjectiles()
    {
        foreach (var projectile in _projectiles.ToArray())
        {
            if (projectile.IsGone) continue;

            // its hitboxes are placed by the systems, so it does nothing on the update it was fired
            if (projectile.Updates++ == 0) continue;

            UpdateProjectile(projectile);
        }

        _projectiles.RemoveAll(x => x.IsGone);
    }

    private void UpdateProjectile(BattleProjectile projectile)
    {
        var body = projectile.Body;
        var state = body.State<ProjectileState>();

        // Activate Arte steps: each arte goes off here and the projectile keeps going
        while (state.Activations.TryDequeue(out var arteId)) Activate(projectile, arteId);
        if (projectile.IsGone) return;

        // a contact projectile goes off with the full arte on the first fighter it touches, and goes off empty on a wall
        if (projectile.Contact != ContactTarget.None)
        {
            if (Contacted(projectile))
            {
                GoOff(projectile, true);
                return;
            }

            if (HitsWall(projectile))
            {
                GoOff(projectile, false);
                return;
            }
        }

        // the Detonate step, and the safety net: a projectile that ran out of routes goes off wherever it stopped. Either
        // way a contact projectile that touched nobody only goes away, unless it's set to detonate at its route's end
        if (state.Detonated || !NextRoute(projectile)) GoOff(projectile, projectile.Contact == ContactTarget.None || projectile.DetonatesAtEnd);
    }

    // keeps the projectile on a route: the one it's on, or the next of its page's routes. False when none are left
    private bool NextRoute(BattleProjectile projectile)
    {
        if (projectile.RouteIndex >= 0 && projectile.RouteIndex < projectile.Routes.Count && movementRouteSystem.IsFollowing(projectile.Body, projectile.Routes[projectile.RouteIndex])) return true;
        if (++projectile.RouteIndex >= projectile.Routes.Count) return false;

        movementRouteSystem.Start(projectile.Body, projectile.Routes[projectile.RouteIndex]);
        return true;
    }

    // the arte the projectile belongs to lands where it is (full), or it just goes away (empty), and it's removed
    private void GoOff(BattleProjectile projectile, bool full)
    {
        if (full) Detonate(projectile.Arte, projectile.Attacker, projectile.Body, Facing(projectile.Body));
        Remove(projectile);
    }

    // an activated arte is owned by the projectile's attacker and costs nothing, with no cast and no cooldown. One with
    // projectiles of its own fires them from here (a bomb bursting into fragments), as deep as ArteChains.MaxDepth. A
    // chain that goes deeper is cut off here, and that's reported so a designer isn't left wondering why it stopped
    private void Activate(BattleProjectile projectile, Guid arteId)
    {
        if (gameDataService.Artes.FirstOrDefault(x => x.Guid == arteId) is not { } arte) return;

        var facing = Facing(projectile.Body);
        if (arte.Projectiles.Count == 0) Detonate(arte, projectile.Attacker, projectile.Body, facing);
        else if (projectile.Depth < ArteChains.MaxDepth) Fire(arte, projectile.Attacker, projectile.Body, facing, projectile.Depth + 1);
        else Debug.WriteLine($"Arte chain cut off: '{arte.Name}' was activated {ArteChains.MaxDepth} projectiles deep from '{projectile.Arte.Name}', so its projectiles were not fired (the limit is {ArteChains.MaxDepth}).");
    }

    private void Remove(BattleProjectile projectile)
    {
        projectile.IsGone = true;
        gameStateService.MapEntities.Remove(projectile.Body);
    }

    // a fighter it detonates on, touched anywhere between last update and this one so a fast projectile can't skip one.
    // Teams are from the side of the fighter who fired it, it never goes off on that fighter, and it passes through
    // the fighters it ignores (and anyone who's down)
    private bool Contacted(BattleProjectile projectile)
    {
        if (Bounds(projectile.Body) is not { } now) return false;

        if (projectile.Body.State<ProjectileState>() is { Jumped: true } jumped)
        {
            jumped.Jumped = false;
            projectile.LastBounds = null;
        }

        var swept = now;
        if (projectile.LastBounds is { } last) swept.Union(last);
        projectile.LastBounds = now;

        var team = projectile.Attacker.Team;
        return Fighters.Any(fighter =>
            !fighter.IsDown &&
            fighter != projectile.Attacker.Source &&
            projectile.Contact switch
            {
                ContactTarget.Enemies => fighter.OriginalTeam != team,
                ContactTarget.Allies => fighter.OriginalTeam == team,
                _ => true
            } &&
            hitboxSystem.Main(fighter.Body) is { } feet &&
            feet.Collide(swept.X, swept.Y, swept.Width, swept.Height));
    }

    // a collision area on its level (the walls around the field) under any of its hitboxes
    private bool HitsWall(BattleProjectile projectile)
    {
        if (projectile.Body.GetModule<PositionModule>() is not { } position) return false;

        var level = HitboxSystem.Level(position);
        var facing = Facing(projectile.Body);
        var boxes = hitboxSystem.PlacedOf(projectile.Body).ToArray();
        return gameStateService.MapAreas.Any(area => area.AreaType == MapAreaType.Collision && area.Level == level && area.Blocks(facing) && boxes.Any(box => hitboxSystem.Overlaps(box, area)));
    }

    // all of the entity's hitboxes where they are now, its feet tile when it has none
    private Rect? Bounds(MapEntity body)
    {
        Rect? bounds = null;
        foreach (var box in hitboxSystem.PlacedOf(body))
        {
            var rect = new Rect(box.X, box.Y, box.Width, box.Height);
            if (bounds is { } sofar) rect.Union(sofar);
            bounds = rect;
        }

        if (bounds is not null) return bounds;

        var feet = FeetTile(body);
        return new Rect(feet.X, feet.Y, Tile, Tile);
    }

    // the AI's area rule: a tile in from the edge, so the target would still be caught a tile farther out. An area that only
    // reaches one tile loses nothing
    private bool WellInside(BattleFighter fighter, BattleFighter target, Rect[] boxes, EffectScope scope)
    {
        if (scope is EffectScope.Self or EffectScope.Party || boxes.Length == 0) return true;
        if (hitboxSystem.Main(target.Body) is not { } body) return false;

        var feet = FeetTile(fighter.Body);
        var reach = boxes.Max(x => Math.Max(Math.Max(feet.X - x.Left, x.Right - (feet.X + Tile)), Math.Max(feet.Y - x.Top, x.Bottom - (feet.Y + Tile))));
        if (reach <= Tile * 1.25) return true;

        var me = Feet(fighter);
        var them = Feet(target);
        var (dx, dy) = (them.X - me.X, them.Y - me.Y);
        var farthest = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (farthest < 1) return true;

        var (outX, outY) = (dx / farthest * Tile, dy / farthest * Tile);
        return boxes.Any(box => body.Collide(box.X - outX, box.Y - outY, box.Width, box.Height));
    }

    // the AI's guess for an arte with projectiles: a tile-wide line from the fighter's feet tile, the way it
    // faces, as long as the farthest projectile steps forward in its routes (one tile at least). A long flight is easy to
    // walk out of, so a range of 6 tiles or more counts 2 less, 4 or 5 counts 1 less, and 3 or less loses nothing
    private bool InFlightLine(BattleFighter fighter, Arte arte, BattleFighter target, Direction facing)
    {
        if (hitboxSystem.Main(target.Body) is not { } body) return false;

        var steps = arte.Projectiles
            .Select(projectile => projectile.Pages.SelectMany(x => x.Actions).OfType<EntityMovementAction>().SelectMany(x => x.Route).OfType<StepForwardMovement>().Sum(x => x.Steps))
            .DefaultIfEmpty(0)
            .Max();
        var tiles = steps * gameDataService.StepSize * gameDataService.ScaleFactor / Tile;
        var reach = Math.Max(1, tiles - (tiles >= 5.5 ? 2 : tiles >= 3.5 ? 1 : 0)) * Tile;
        var feet = FeetTile(fighter.Body);
        var line = facing switch
        {
            Direction.Up => new Rect(feet.X, feet.Y - reach, Tile, reach + Tile),
            Direction.Left => new Rect(feet.X - reach, feet.Y, reach + Tile, Tile),
            Direction.Right => new Rect(feet.X, feet.Y, reach + Tile, Tile),
            _ => new Rect(feet.X, feet.Y, Tile, reach + Tile)
        };

        return body.Collide(line.X, line.Y, line.Width, line.Height);
    }

    private static IEnumerable<EntityMovementAction> Routes(GameLibrary.Models.Entity projectile) => projectile.Pages.SelectMany(x => x.Actions).OfType<EntityMovementAction>();

    // the sheets of every projectile the fighters' artes can fire (and the artes those activate), measured so the
    // pictures show and stand on their tile from the first shot
    private async Task MeasureProjectiles(IEnumerable<Arte> artes)
    {
        var seen = new HashSet<Guid>();
        var waiting = new Queue<Arte>(artes);
        var images = new HashSet<string>();
        while (waiting.TryDequeue(out var arte))
        {
            if (!seen.Add(arte.Guid)) continue;

            foreach (var page in arte.Projectiles.SelectMany(x => x.Pages))
            {
                foreach (var image in page.Modules.OfType<CharacterImageModule>().Select(x => x.ImageName).OfType<string>()) images.Add(image);

                var activated = page.Actions.OfType<EntityMovementAction>().SelectMany(x => x.Route).OfType<ActivateArteMovement>().Select(x => x.ArteId);
                foreach (var id in activated)
                {
                    if (gameDataService.Artes.FirstOrDefault(x => x.Guid == id) is { } next) waiting.Enqueue(next);
                }
            }
        }

        foreach (var image in images.Where(x => !_standOffsets.ContainsKey(x)))
        {
            await characterImageSystem.Measure(image);
            var (x, y) = await playerSpawner.StandOnTile(image, 0, 0);
            _standOffsets[image] = new Point(x, y);
        }
    }

    #endregion

    #region Private Methods

    // the first update of a battle: everyone's keys and who drives them, nothing on cooldown
    private void Begin(ActiveBattle battle)
    {
        _battle = battle;
        _endTimer = 0;
        _won = false;
        _summarized = false;
        _lost = false;
        _gameOver = false;
        _fleeProgress = 0;
        _endWait = EndDelay;
        _rewards.Clear();
        Remember(battle);
        partyStates.CarryIn(battle.Fighters);
        _projectiles.Clear();
        drops.Clear();
        playerController.Reset();
        FillItemSlots(battle);
        foreach (var fighter in battle.Fighters)
        {
            fighter.Slots = SlotsFor(fighter);
            fighter.Controller = fighter.IsLeader ? playerController : aiController;
        }

        _ = MeasureProjectiles(battle.Fighters.SelectMany(x => x.Slots).OfType<Arte>());
    }

    // the item keys start with the first consumables in the bag that can be used in battle, in the Items list's order. The
    // menu will let the player choose later
    private void FillItemSlots(ActiveBattle battle)
    {
        Array.Clear(battle.ItemSlots);
        battle.ItemCooldown = 0;
        battle.ItemCooldownTotal = 0;

        var usable = gameDataService.Consumables
            .Where(x => (x.ItemUse is ItemUse.Anywhere or ItemUse.BattleOnly) && gameStateService.Items.GetValueOrDefault(x.Guid) > 0)
            .Take(battle.ItemSlots.Length)
            .ToArray();
        for (var i = 0; i < usable.Length; i++) battle.ItemSlots[i] = usable[i];
    }

    // the last of an item is used: every key holding it empties, and the cooldown carries on
    private static void ClearItem(ActiveBattle battle, Consumable item)
    {
        for (var i = 0; i < battle.ItemSlots.Length; i++)
        {
            if (battle.ItemSlots[i] == item) battle.ItemSlots[i] = null;
        }
    }

    // the item lands around the user: its hitboxes (drawn facing down, and they don't turn) from the user's feet tile,
    // on whoever its scope lets it touch, the user included
    private void LandItem(Consumable item, BattleFighter user)
    {
        if (_battle is not { } battle) return;

        var attacker = attributes.Snapshot(user);
        var boxes = ItemBoxes(item, user);
        debugOverlay.FlashArteHitboxes(boxes.Select(x => (x.X, x.Y, x.Width, x.Height)));

        var revives = item.ClearAllStates || item.RemoveStates.Contains(Incapacitated());
        var targets = battle.Fighters.Where(x => InScope(item.EffectScope, user, attacker.Team, x, revives) && Touches(x, boxes, item.EffectScope)).ToArray();
        foreach (var target in targets)
        {
            animationSystem.Play(target.Body, item.TargetAnimation);
            LandItemOn(item, attacker, target);
        }
    }

    private Rect[] ItemBoxes(Consumable item, BattleFighter user)
    {
        var scale = gameDataService.ScaleFactor;
        var feet = FeetTile(user.Body);
        return item.Hitboxes.Select(x => new Rect(feet.X + (x.OffsetX * scale), feet.Y + (x.OffsetY * scale), x.Width * scale, x.Height * scale)).ToArray();
    }

    // one item landing on one fighter: states first (a revive brings the fallen back before anything heals them), then Life
    // and Mana. An item never misses, ignores Defense, Power and elements, and never crits: it's a flat amount plus a
    // percent of the target's maximum, spread by Variance. Negative restores, as everywhere else
    private void LandItemOn(Consumable item, AttackerSnapshot attacker, BattleFighter target)
    {
        var wasDown = target.IsDown;
        if (item.ClearAllStates)
        {
            target.States.Clear();
            if (target.IsDown) GetUp(target);
        }

        foreach (var id in item.RemoveStates)
        {
            if (id != Incapacitated()) states.Remove(target, id);
            else if (target.IsDown) GetUp(target);
        }

        if (item.RemoveNegativeStates) states.RemoveNegative(target);
        foreach (var id in item.AddStates) states.Add(target, id, attacker, item.StateChances.GetValueOrDefault(id, 100));

        // a revive that heals nothing still gets them up with 1 Life
        if (wasDown && !target.IsDown && target.Stats.Life <= 0) target.Stats.Life = 1;

        var stats = target.Stats;
        if (item.HpDamage != 0 || item.HpPercent != 0)
        {
            var amount = ItemAmount(item, item.HpDamage, item.HpPercent, stats.MaxLife);
            var before = stats.Life;
            stats.Life = Math.Clamp(stats.Life - amount, 0, stats.MaxLife);
            var change = before - stats.Life;
            attacker.Source.Threat += change > 0 ? change : -change * 0.5;
            if (amount >= 0) battleHud.Popup(change.ToString(), Head(target), HudPopupKind.Damage);
            else battleHud.Popup((-change).ToString(), Head(target), HudPopupKind.Heal);
        }

        if (item.ManaDamage != 0 || item.ManaPercent != 0)
        {
            var amount = ItemAmount(item, item.ManaDamage, item.ManaPercent, stats.MaxMana);
            var before = stats.Mana;
            stats.Mana = Math.Clamp(stats.Mana - amount, 0, stats.MaxMana);
            var change = before - stats.Mana;
            if (change != 0) battleHud.Popup($"{(change < 0 ? "+" : "-")}{Math.Abs(change)} {gameDataService.GameSettings.ManaName}", Head(target), change < 0 ? HudPopupKind.Heal : HudPopupKind.Damage);
        }

        if (stats.Life <= 0 && !target.IsDown) Fall(target);
    }

    // flat plus a percent of the maximum, then spread by the item's Variance (never more than half either way)
    private static int ItemAmount(Consumable item, int flat, int percent, int maximum)
    {
        var value = flat + (percent / 100.0 * maximum);
        var variance = Math.Clamp(item.Variance / 100.0, 0, 0.5);
        value *= 1 + (((Random.Shared.NextDouble() * 2) - 1) * variance);
        return (int) Math.Round(value);
    }

    // slot 0 is the weapon's attack, then the artes the fighter knows, in the order they were learned
    private Arte?[] SlotsFor(BattleFighter fighter)
    {
        var slots = new Arte?[PlayerController.SlotKeys.Length];
        var known = new List<Guid>();
        if (fighter.Stats.Equipment.GetValueOrDefault("Weapon")?.Id is { } weaponId &&
            gameDataService.Equipment.FirstOrDefault(x => x.Guid == weaponId)?.Arte is { } weaponArte) known.Add(weaponArte);

        known.AddRange(fighter.Stats.Artes);
        known.AddRange(fighter.Stats.ConditionalArtes.Where(x => Learned(fighter, x)).Select(x => x.ArteGuid).OfType<Guid>());

        var artes = known.Distinct().Select(id => gameDataService.Artes.FirstOrDefault(x => x.Guid == id)).OfType<Arte>().Take(slots.Length).ToArray();
        for (var i = 0; i < artes.Length; i++) slots[i] = artes[i];
        return slots;
    }

    // a conditional arte is known once every condition is met. In battle only Level conditions are checked, against the
    // fighter's own level (an enemy learns its stronger artes as it grows); any other kind of condition keeps it unknown
    private static bool Learned(BattleFighter fighter, GameLibrary.Models.Fighter.FighterArte arte)
    {
        return arte.Conditions.All(x => x is GameLibrary.Models.Conditions.LevelCondition level && fighter.Stats.Level >= level.Level);
    }

    // cooldowns run down and casts finish
    private void Tick(BattleFighter fighter)
    {
        if (fighter.GlobalCooldown > 0) fighter.GlobalCooldown--;
        foreach (var (id, (left, total)) in fighter.Cooldowns.ToArray())
        {
            if (left <= 1) fighter.Cooldowns.Remove(id);
            else fighter.Cooldowns[id] = (left - 1, total);
        }

        TickStates(fighter);

        if (fighter.Casting is not { } arte || fighter.IsDown) return;
        if (--fighter.CastLeft > 0) return;

        fighter.Casting = null;
        Lock(fighter, false);
        Release(fighter, arte);
    }

    // states wear down, and the ones due activate their Tick Arte on the holder, owned by whoever applied the state
    private void TickStates(BattleFighter fighter)
    {
        if (fighter.IsDown) return;

        foreach (var each in states.Update(fighter))
        {
            if (fighter.IsDown) return;

            // the state's animation plays on the holder every tick, whether or not there's an arte to go with it
            if (each.State.TickAnimation != Guid.Empty) animationSystem.Play(fighter.Body, each.State.TickAnimation);
            if (gameDataService.Artes.FirstOrDefault(x => x.Guid == each.State.TickArte) is { } arte) Detonate(arte, each.Attacker, fighter.Body, Facing(fighter), fighter);
        }

        // holding a stun-lock state means doing nothing: no walking or turning (see BattleLockState), and no artes unless
        // the state allows them, which also breaks a cast in progress
        var lockState = fighter.Body.State<BattleLockState>();
        lockState.Stunned = !fighter.IsDown && states.IsStunned(fighter);
        fighter.Body.State<MovementState>().SpeedFactor = states.SpeedFactor(fighter);
        if (fighter.Casting is not null && states.BlocksArtes(fighter))
        {
            fighter.Casting = null;
            Lock(fighter, false);
        }
    }

    // ready: not casting, not on cooldown, and enough Mana. The Mana is paid when the cast starts
    public void Use(BattleFighter fighter, Arte arte)
    {
        if (!CanUse(fighter, arte)) return;

        fighter.Stats.Mana -= arte.Cost;
        fighter.GlobalCooldown = GlobalCooldown;
        if (arte.Cooldown > 0) fighter.Cooldowns[arte.Guid] = (arte.Cooldown, arte.Cooldown);
        PlayCasterAnimation(fighter, arte);

        if (arte.CastTime <= 0)
        {
            Release(fighter, arte);
            return;
        }

        fighter.Casting = arte;
        fighter.CastLeft = fighter.CastTotal = arte.CastTime;
        Lock(fighter, true);
    }

    // the cast is done: an arte with projectiles fires all of them and does nothing else, one without lands at the user
    private void Release(BattleFighter user, Arte arte)
    {
        // the arte's own Threat, for tanks and Threat drops. Nobody goes below none
        user.Threat = Math.Max(0, user.Threat + arte.Threat);

        var attacker = attributes.Snapshot(user);
        var facing = Facing(user);
        if (arte.Projectiles.Count > 0) Fire(arte, attacker, user.Body, facing, 0);
        else Detonate(arte, attacker, user.Body, facing);
    }

    // the arte lands at the origin (the user, or a projectile): its hitboxes from the origin's feet tile, turned the
    // way it faces, owned by the attacker. Landing never fires projectiles, only casting or activating an arte does
    private void Detonate(Arte arte, AttackerSnapshot attacker, MapEntity origin, Direction facing, BattleFighter? holder = null)
    {
        if (_battle is not { } battle) return;

        var boxes = Hitboxes(origin, arte, facing).ToArray();
        debugOverlay.FlashArteHitboxes(boxes.Select(x => (x.X, x.Y, x.Width, x.Height)));
        var revives = arte.RemoveStates.Contains(Incapacitated());
        // a state's tick lands on the fighter holding the state, whatever the arte's scope says
        var targets = holder is not null
            ? new[] { holder }
            : battle.Fighters.Where(x => InScope(arte.EffectScope, attacker.Source, attacker.Team, x, revives) && Touches(x, boxes, arte.EffectScope)).ToArray();
        var targetAnimation = Turned(arte, arte.TargetAnimation, facing);
        var targetPlacement = MovesAndRotates(arte) ? arte.AnimationFor(facing).WithoutOffset() : null;

        foreach (var target in targets)
        {
            // the target animation turns with the user, but it's never moved: it plays where the target was hit
            animationSystem.Play(target.Body, targetAnimation, targetPlacement);
            Land(arte, attacker, target, holder is null ? origin : null);
        }
    }

    // origin is where the hit came from (the user, or the projectile that detonated), and null for a state's tick, which
    // never counts as from behind
    private void Land(Arte arte, AttackerSnapshot attacker, BattleFighter target, MapEntity? origin)
    {
        // once the hit lands: states are removed (a revive brings the fighter back first) and then added, so they count
        // toward the arte's own damage
        var outcome = hitCalculator.Resolve(arte, attacker, target, () => ApplyStates(arte, attacker, target), origin is not null && IsBehind(target, origin), tick: origin is null);
        if (outcome.Missed)
        {
            battleHud.Popup("Miss", Head(target), HudPopupKind.Miss);
            return;
        }

        // a taunted party member isn't forced to do anything, but hurts anyone else far less (a state's tick isn't aimed)
        if (outcome.LifeChange > 0 && origin is not null && !attacker.Source.IsEnemy && states.TauntedBy(attacker.Source) is { } taunter && taunter != target)
        {
            outcome = outcome with { LifeChange = (int) Math.Round(outcome.LifeChange * TauntedDamage) };
        }

        var stats = target.Stats;
        var before = stats.Life;
        stats.Life = Math.Clamp(stats.Life - outcome.LifeChange, 0, stats.MaxLife);
        var change = before - stats.Life;

        // Threat: all the damage dealt, half of the healing actually done (overheal doesn't count). Hurting yourself (a
        // carried poison whose enemy is gone) builds none
        if (attacker.Source != target || change < 0) attacker.Source.Threat += change > 0 ? change : -change * 0.5;

        // a heal shows green even when it restores nothing (the target was already full), a crit shows red on a hit and
        // blue on a heal
        if (outcome.LifeChange >= 0) battleHud.Popup(change.ToString(), Head(target), outcome.Critical ? HudPopupKind.Critical : HudPopupKind.Damage);
        else battleHud.Popup((-change).ToString(), Head(target), outcome.Critical ? HudPopupKind.CriticalHeal : HudPopupKind.Heal);

        if (stats.Life <= 0 && !target.IsDown) Fall(target);
    }

    // the hit came from behind when its origin is on the back side of the target's feet tile: opposite the way it faces,
    // and farther back than it is to the side (so the back is a quarter-turn wide, the way the front is)
    private bool IsBehind(BattleFighter target, MapEntity origin)
    {
        var from = FeetTile(origin);
        var at = FeetTile(target.Body);
        var dx = from.X - at.X;
        var dy = from.Y - at.Y;

        return Facing(target) switch
        {
            Direction.Up => dy > 0 && dy >= Math.Abs(dx),
            Direction.Left => dx > 0 && dx >= Math.Abs(dy),
            Direction.Right => dx < 0 && -dx >= Math.Abs(dy),
            _ => dy < 0 && -dy >= Math.Abs(dx)
        };
    }

    private void ApplyStates(Arte arte, AttackerSnapshot attacker, BattleFighter target)
    {
        foreach (var id in arte.RemoveStates)
        {
            if (id == Incapacitated())
            {
                if (target.IsDown) GetUp(target);
            }
            else
            {
                states.Remove(target, id);
            }
        }

        if (arte.RemoveNegativeStates) states.RemoveNegative(target);

        foreach (var id in arte.AddStates) states.Add(target, id, attacker, arte.StateChances.GetValueOrDefault(id, 100));
    }

    // Incapacitated: every state goes and Incapacitated goes on, it can't move, nobody bumps into it, and enemies fade away.
    // Every projectile it fired disappears with it (a boss's meteor storm can't kill the party during the victory screen)
    private void Fall(BattleFighter fighter)
    {
        fighter.IsDown = true;
        fighter.Casting = null;
        fighter.Threat = 0;
        states.Incapacitate(fighter, attributes.Snapshot(fighter));
        if (_battle is { } battle) states.SourceFell(battle.Fighters, fighter);
        foreach (var projectile in _projectiles.Where(x => !x.IsGone && x.Attacker.Source == fighter)) Remove(projectile);
        Lock(fighter, true);
        Solid(fighter, false);
        movementRouteSystem.FadeTo(fighter.Body, fighter.IsEnemy ? 0 : 0.4, 400);

        // an enemy leaves its money and drops where it fell, and they stay until someone picks them up or the battle ends
        if (fighter.IsEnemy) drops.Spawn(fighter.Stats, Feet(fighter), (int) (fighter.Body.GetModule<PositionModule>()?.SortZ ?? 0));
    }

    // a party member whose feet are over a pickup takes it: money goes to the party and an item into its bag
    private void Collect(ActiveBattle battle)
    {
        // a lost battle leaves everything on the floor
        if (_lost) return;

        var collectors = battle.Fighters.Where(x => !x.IsEnemy && !x.IsDown).ToArray();
        foreach (var (pickup, who) in drops.Update([.. collectors.Select(Feet)], Tile * 0.6))
        {
            if (pickup.ItemId is { } item)
            {
                partyService.ChangeItem(item, pickup.Amount);
                _rewards.AddItem(item, pickup.Name, pickup.Icon, pickup.Amount);
            }
            else
            {
                gameStateService.Money += pickup.Amount;
                _rewards.Money += pickup.Amount;
            }

            battleHud.Popup(pickup.ItemId is null ? $"+{pickup.Name}" : pickup.Name, Head(collectors[who]), HudPopupKind.Heal);
        }
    }

    private void GetUp(BattleFighter fighter)
    {
        fighter.IsDown = false;
        states.Remove(fighter, Incapacitated());
        Lock(fighter, false);
        Solid(fighter, true);
        movementRouteSystem.FadeTo(fighter.Body, 1, 200);
    }

    // holding Shift runs away (only in a battle that can be run from, and only while the leader is on their feet): a bar
    // fills over a few seconds and letting go empties it at once. When it's full the escape can still fail and the fight
    // carries on, and trying again costs nothing. A successful escape ends the battle with no EXP, no spoils and no
    // victory screen
    private void UpdateFlee(ActiveBattle battle)
    {
        var leader = battle.Fighters.FirstOrDefault(x => x.IsLeader);
        var holding = CanRun(battle) && !_won && !_lost && leader is { IsDown: false } && inputService.Shift;
        _fleeProgress = holding ? _fleeProgress + 1 : 0;
        battle.FleeFraction = Math.Min(1.0, _fleeProgress / (double) FleeUpdates);
        if (_fleeProgress < FleeUpdates || leader is null) return;

        _fleeProgress = 0;
        battle.FleeFraction = 0;
        if (Random.Shared.NextDouble() < FleeFailChance)
        {
            battleHud.Popup("Couldn't escape!", Head(leader), HudPopupKind.Miss);
            return;
        }

        battleHud.Popup("Escaped!", Head(leader), HudPopupKind.Heal);
        End(battle);
    }

    // running away is the troop's setting (Can Run on the Troops page), so it's the same wherever the fight starts from
    private static bool CanRun(ActiveBattle battle) => battle.Request.Troop.CanRun;

    // victory when every enemy is down, defeat when every party member is. The first one settled is final, and a win comes
    // first: if both sides fall in the same update the party won. A party that's lost stays lost even if the last enemy
    // then falls to something lingering (a poison it left behind), so defeat never shows Victory
    private void CheckForEnd(ActiveBattle battle)
    {
        var enemiesDown = battle.Fighters.Where(x => x.OriginalTeam == BattleFighter.EnemyTeam).All(x => x.IsDown);
        var partyDown = battle.Fighters.Where(x => x.OriginalTeam == BattleFighter.PartyTeam).All(x => x.IsDown);
        if (!_won && !_lost)
        {
            if (enemiesDown) Win(battle);
            else if (partyDown) Lose();
        }

        if (_lost)
        {
            Defeat(battle);
            return;
        }

        if (!_won)
        {
            _endTimer = 0;
            return;
        }

        // a won battle waits out its whole pause, so the party can pick up what the enemies left and nothing cuts it short
        if (++_endTimer < _endWait) return;

        // a win shows the victory screen first, and the battle ends when it's closed. Anything still lying there is lost
        _summarized = true;
        drops.Clear();
        victoryScreen.Show(_rewards);
    }

    // every party member on the field is down: the party lost. Nobody is left to pick anything up, so what the enemies
    // that fell dropped is lost, and no EXP or spoils are handed out
    private void Lose()
    {
        _lost = true;
        _endTimer = 0;
        drops.Clear();
    }

    // after the same short pause a win has, a battle that can't be lost for good just ends and the story carries on (the
    // fallen get up with 1 Life), and any other opens the game over screen. What follows is settled when it's closed
    private void Defeat(ActiveBattle battle)
    {
        if (++_endTimer < EndDelay) return;

        if (CanLose(battle))
        {
            End(battle);
            return;
        }

        _gameOver = true;
        gameOverScreen.Show(gameDataService.GameSettings.Defeat);
    }

    // a scripted fight (a tutorial, a story loss) is a troop marked Can Lose (Troops page), and losing it is never a game
    // over, wherever the fight starts from
    private static bool CanLose(ActiveBattle battle) => battle.Request.Troop.CanLose;

    // the game over screen was closed. Retry puts the party, the money and the bag back the way they were when the fight
    // began and asks for the same fight again once the party is back on the story map. Game Over is the game over
    // sequence's business (it has nowhere to go yet, so it just returns the party to the story map)
    private void Settle(ActiveBattle battle)
    {
        if (gameDataService.GameSettings.Defeat != DefeatOutcome.Retry)
        {
            gameOverSequence.Play(() => End(battle));
            return;
        }

        End(battle);
        foreach (var (fighter, life, mana) in _startStats)
        {
            fighter.Stats.Life = life;
            fighter.Stats.Mana = mana;
        }

        gameStateService.Money = _startMoney;
        gameStateService.Items.Clear();
        foreach (var (id, count) in _startItems) gameStateService.Items[id] = count;
        partyStates.Restore(_startStates);

        battle.Retry = true;
    }

    // how the party stands as the fight begins, for a retry
    private void Remember(ActiveBattle battle)
    {
        _startStats.Clear();
        _startStats.AddRange(battle.Fighters.Where(x => !x.IsEnemy).Select(x => (x, x.Stats.Life, x.Stats.Mana)));
        _startMoney = gameStateService.Money;
        _startItems.Clear();
        foreach (var (id, count) in gameStateService.Items) _startItems[id] = count;
        _startStates = partyStates.Snapshot();
    }

    // the battle is officially won the moment the last enemy falls: everyone on the field is on their feet with every state
    // cleared, and EXP goes out in full to everyone in the party except the ones who were down right then. The party
    // members not in the fight get it too
    private void Win(ActiveBattle battle)
    {
        _won = true;
        mapAudio.FadeOutBgm(MusicFadeSeconds);
        _endWait = drops.Count > 0 ? PickupDelay : EndDelay;

        var experience = battle.Fighters.Where(x => x.OriginalTeam == BattleFighter.EnemyTeam).Sum(x => x.Stats.Experience);
        _rewards.Experience = experience;
        var fallen = battle.Fighters.Where(x => !x.IsEnemy && x.IsDown).Select(x => x.Stats).ToHashSet();
        foreach (var id in gameStateService.OwnedParty)
        {
            if (partyService.Actor(id) is not { } actor) continue;

            if (fallen.Contains(actor))
            {
                _rewards.Members.Add(new BattleRewards.Member(actor.Name, actor.FaceName, actor.FaceIndex, 0, actor.Level, actor.Level, true));
                continue;
            }

            var before = actor.Level;
            var levels = partyService.GainExperience(actor, experience);
            _rewards.Members.Add(new BattleRewards.Member(actor.Name, actor.FaceName, actor.FaceIndex, experience, before, actor.Level, false));
            if (battle.Fighters.FirstOrDefault(x => x.Stats == actor) is not { } onField) continue;

            if (experience > 0) battleHud.Popup($"+{experience} EXP", Head(onField), HudPopupKind.Heal);
            if (levels <= 0) continue;

            // the level-up recovery clears the states too, even the ones that would have remained after the battle
            onField.States.Clear();
            battleHud.Popup($"Level {actor.Level}!", Head(onField), HudPopupKind.Heal);
        }

        foreach (var fighter in battle.Fighters)
        {
            states.EndOfBattle(fighter);
            if (fighter.IsEnemy || !fighter.IsDown) continue;

            fighter.Stats.Life = 1;
            GetUp(fighter);
        }
    }

    // the battle officially ends: the fallen get up with 1 Life, and the party goes back to the story map
    private void End(ActiveBattle battle)
    {
        foreach (var projectile in _projectiles) Remove(projectile);
        _projectiles.Clear();

        // whatever wasn't picked up is lost
        drops.Clear();

        foreach (var fighter in battle.Fighters) states.EndOfBattle(fighter);

        // what remains (and what's left of its clock) goes back with the party
        partyStates.CarryOut(battle.Fighters);

        foreach (var fighter in battle.Fighters.Where(x => !x.IsEnemy))
        {
            if (fighter.Stats.Life <= 0) fighter.Stats.Life = 1;
        }

        battle.IsEnding = true;
        gameStateService.EdgeTransfer = new EdgeTransfer(battle.ReturnMap, battle.ReturnX, battle.ReturnY, battle.ReturnDirection);
    }

    private static void Lock(BattleFighter fighter, bool locked) => fighter.Body.State<BattleLockState>().Locked = locked;

    // a fighter that's down can be walked through (its hitbox still counts for a revive)
    private static void Solid(BattleFighter fighter, bool solid)
    {
        foreach (var hitboxes in fighter.Body.Entity.Pages.SelectMany(x => x.Modules).OfType<EntityHitboxModule>()) hitboxes.Collide = solid;
    }

    // who an arte's scope lets it touch: teams are from the user's side (the team they were on when they used it, which
    // a projectile remembers), and a target is judged by its Original Team
    private static bool InScope(EffectScope scope, BattleFighter user, int team, BattleFighter target, bool revives)
    {
        var ally = target.OriginalTeam == team;
        if (target.IsDown && !revives) return false;

        return scope switch
        {
            EffectScope.Enemies => !ally,
            EffectScope.Allies => ally,
            EffectScope.Self => target == user,
            EffectScope.Party => ally,
            EffectScope.DeadAlly => ally && target.IsDown,
            EffectScope.DeadEnemy => !ally && target.IsDown,
            EffectScope.All or EffectScope.AllNotDead or EffectScope.AllDead => true,
            _ => false
        };
    }

    // Self and Party don't need the hitbox, everything else has to be inside one of them
    private bool Touches(BattleFighter target, Rect[] boxes, EffectScope scope)
    {
        if (scope is EffectScope.Self or EffectScope.Party) return true;
        if (hitboxSystem.Main(target.Body) is not { } body) return false;

        return boxes.Any(box => body.Collide(box.X, box.Y, box.Width, box.Height));
    }

    // the arte's hitboxes in scaled map pixels, drawn from the tile the origin stands on (the user, or a projectile where
    // it detonates). An arte that turns with its user uses the hitboxes drawn for the way they face, exactly as drawn. A
    // direction with none of its own turns the down hitboxes about the tile's middle instead, and facing up those are
    // measured from the top of the origin's hitbox, so an up swing touches the body instead of leaving a gap above it
    private IEnumerable<Rect> Hitboxes(MapEntity origin, Arte arte, Direction facing)
    {
        var scale = gameDataService.ScaleFactor;
        var feet = FeetTile(origin);
        var turn = arte.DirectionRotate ? facing : Direction.Down;

        var drawn = arte.HitboxesFor(turn);
        if (turn == Direction.Down || drawn.Count > 0)
        {
            foreach (var box in drawn) yield return new Rect(feet.X + (box.OffsetX * scale), feet.Y + (box.OffsetY * scale), box.Width * scale, box.Height * scale);
            yield break;
        }

        var pushY = turn == Direction.Up && hitboxSystem.Main(origin) is { } body ? body.Y - feet.Y : 0.0;
        foreach (var box in arte.Hitboxes.Select(x => Arte.TurnedFromDown(x, turn, gameDataService.GridSize)))
        {
            yield return new Rect(feet.X + (box.OffsetX * scale), feet.Y + (box.OffsetY * scale) + pushY, box.Width * scale, box.Height * scale);
        }
    }

    // the top left of the tile the body stands on: under the middle of the sprite's bottom
    private Point FeetTile(MapEntity body)
    {
        var scale = gameDataService.ScaleFactor;
        var tile = gameDataService.GridSize * scale;
        var position = body.GetModule<PositionModule>();
        var sprite = body.State<CharacterImageState>().Sprite;
        var (width, height) = sprite is null ? (tile, tile) : (sprite.Width * scale, sprite.Height * scale);
        return new Point((position?.X ?? 0) + ((width - tile) / 2.0), (position?.Y ?? 0) + height - tile);
    }

    // just over the head, where a damage number starts
    private Point Head(BattleFighter fighter)
    {
        var sprite = fighter.Body.State<CharacterImageState>().Sprite;
        return sprite is null ? default : new Point(sprite.X + (sprite.Width * gameDataService.ScaleFactor / 2.0), sprite.Y);
    }

    private static Direction Facing(BattleFighter fighter) => Facing(fighter.Body);

    private static Direction Facing(MapEntity body)
    {
        return body.GetModule<PositionModule>() is { } position ? (Direction) (Math.Max(0, (int) position.Direction) % 4) : Direction.Down;
    }

    // the caster animation on the user: one animation moved and turned for the way they face, or the animation for
    // that way picked by Id
    private void PlayCasterAnimation(BattleFighter fighter, Arte arte)
    {
        var facing = Facing(fighter);
        if (!MovesAndRotates(arte))
        {
            animationSystem.Play(fighter.Body, Turned(arte, arte.CasterAnimation, facing));
            return;
        }

        animationSystem.Play(fighter.Body, arte.CasterAnimation, arte.AnimationFor(facing));
    }

    private static bool MovesAndRotates(Arte arte) => arte is { DirectionRotate: true, DirectionAnimations: DirectionAnimations.MoveAndRotate };

    // "Rotate Arte With Character Direction" picks the animation for the way the user faces by animation Id: the arte's
    // own animation facing down, Id + 1 facing left, + 2 facing right and + 3 facing up. By Id, not by place in the
    // Animations list, which is loaded in file order. When there's no animation with that Id, the arte's own one plays
    private Guid? Turned(Arte arte, Guid? animationId, Direction facing)
    {
        if (!arte.DirectionRotate || MovesAndRotates(arte) || gameDataService.Animations.FirstOrDefault(x => x.Guid == animationId) is not { } animation) return animationId;

        var turnedId = animation.Id + (int) facing;
        return gameDataService.Animations.FirstOrDefault(x => x.Id == turnedId)?.Guid ?? animationId;
    }

    private Guid Incapacitated() => states.IncapacitatedId;

    #endregion
}
