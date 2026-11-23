using Windows.Foundation;
using Windows.System;
using GameEngine.Battle.Controllers;
using GameEngine.Interface;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Battle;

/// <summary>
/// Runs the fight once an update while a battle is on and nothing pauses it. Each fighter's controller decides what it
/// does (the player for the leader, the AI for everyone else) through the actions here, so the rules are the same for
/// everyone: casting (a cast locks the caster in place and can't be cancelled), detonating at the user (the arte's
/// hitboxes laid out from the user's feet tile, turned the way they face), every hit worked out by the hit calculator,
/// Life, Threat, falling, and victory or defeat.
/// </summary>
public class BattleRunner(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    HitboxSystem hitboxSystem,
    AnimationSystem animationSystem,
    MovementRouteSystem movementRouteSystem,
    BattleAttributes attributes,
    HitCalculator hitCalculator,
    MovementSystem movementSystem,
    PlayerController playerController,
    PlaceholderAiController aiController,
    BattleHud battleHud,
    DebugOverlay debugOverlay) : IBattleActions
{
    #region Constants

    // the tiny cooldown after any arte, only so one key press can't fire two
    private const int GlobalCooldown = 8;

    // the pause between the last enemy falling and the battle ending (drops get picked up here later)
    private const int EndDelay = 60;

    private const string IncapacitatedName = "Incapacitated";

    #endregion

    #region Fields

    private ActiveBattle? _battle;
    private int _endTimer;

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

        foreach (var fighter in battle.Fighters) Tick(fighter);
        foreach (var fighter in battle.Fighters.Where(x => !x.IsDown)) fighter.Controller?.Update(fighter, this);

        CheckForEnd(battle);
    }

    #endregion

    #region Battle Actions

    public IReadOnlyList<BattleFighter> Fighters => _battle?.Fighters ?? [];

    public double Tile => gameDataService.GridSize * gameDataService.ScaleFactor;

    public bool IsOpponent(BattleFighter fighter, BattleFighter target) => target.OriginalTeam != fighter.Team;

    public bool CanUse(BattleFighter fighter, Arte arte)
    {
        return !fighter.IsDown && fighter.Casting is null && fighter.GlobalCooldown <= 0 && !fighter.Cooldowns.ContainsKey(arte.Guid) && fighter.Stats.Mana >= arte.Cost;
    }

    public bool WouldHit(BattleFighter fighter, Arte arte, BattleFighter target, Direction facing)
    {
        var revives = arte.RemoveStates.Contains(Incapacitated());
        return InScope(arte.EffectScope, fighter, target, revives) && Touches(target, [.. Hitboxes(fighter, arte, facing)], arte.EffectScope);
    }

    public void Walk(BattleFighter fighter, Direction direction)
    {
        if (fighter.IsDown || fighter.Body.State<BattleLockState>().Locked) return;

        movementSystem.Move(fighter.Body, direction);
    }

    // the sheet row picks the character, only the way it faces changes
    public void Face(BattleFighter fighter, Direction direction)
    {
        if (fighter.Body.GetModule<PositionModule>() is not { } position) return;

        position.Direction = (Direction) ((Math.Max(0, (int) position.Direction) / 4 * 4) + (int) direction);
    }

    public Point Feet(BattleFighter fighter)
    {
        var tile = FeetTile(fighter.Body);
        return new Point(tile.X + (Tile / 2.0), tile.Y + (Tile / 2.0));
    }

    #endregion

    #region Private Methods

    // the first update of a battle: everyone's keys and who drives them, nothing on cooldown
    private void Begin(ActiveBattle battle)
    {
        _battle = battle;
        _endTimer = 0;
        playerController.Reset();
        foreach (var fighter in battle.Fighters)
        {
            fighter.Slots = SlotsFor(fighter);
            fighter.Controller = fighter.IsLeader ? playerController : aiController;
        }
    }

    // slot 0 is the weapon's attack, then the artes the fighter knows, in the order they were learned
    private Arte?[] SlotsFor(BattleFighter fighter)
    {
        var slots = new Arte?[PlayerController.SlotKeys.Length];
        var known = new List<Guid>();
        if (fighter.Stats.Equipment.GetValueOrDefault("Weapon")?.Id is { } weaponId &&
            gameDataService.Equipment.FirstOrDefault(x => x.Guid == weaponId)?.Arte is { } weaponArte) known.Add(weaponArte);

        known.AddRange(fighter.Stats.Artes);
        known.AddRange(fighter.Stats.ConditionalArtes.Where(x => x.Conditions.Count == 0).Select(x => x.ArteGuid).OfType<Guid>());

        var artes = known.Distinct().Select(id => gameDataService.Artes.FirstOrDefault(x => x.Guid == id)).OfType<Arte>().Take(slots.Length).ToArray();
        for (var i = 0; i < artes.Length; i++) slots[i] = artes[i];
        return slots;
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

        if (fighter.Casting is not { } arte || fighter.IsDown) return;
        if (--fighter.CastLeft > 0) return;

        fighter.Casting = null;
        Lock(fighter, false);
        Detonate(fighter, arte);
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
            Detonate(fighter, arte);
            return;
        }

        fighter.Casting = arte;
        fighter.CastLeft = fighter.CastTotal = arte.CastTime;
        Lock(fighter, true);
    }

    // the arte lands at the user: its hitboxes from the user's feet tile, turned the way the user faces.
    // (Artes with projectiles fire them instead, that's the next step; for now they land at the user too)
    private void Detonate(BattleFighter user, Arte arte)
    {
        if (_battle is not { } battle) return;

        var attacker = attributes.Snapshot(user);
        var facing = Facing(user);
        var boxes = Hitboxes(user, arte, facing).ToArray();
        debugOverlay.FlashArteHitboxes(boxes.Select(x => (x.X, x.Y, x.Width, x.Height)));
        var revives = arte.RemoveStates.Contains(Incapacitated());
        var targets = battle.Fighters.Where(x => InScope(arte.EffectScope, user, x, revives) && Touches(x, boxes, arte.EffectScope)).ToArray();
        var targetAnimation = Turned(arte, arte.TargetAnimation, facing);
        var targetPlacement = MovesAndRotates(arte) ? arte.AnimationFor(facing).WithoutOffset() : null;

        foreach (var target in targets)
        {
            // the target animation turns with the user, but it's never moved: it plays where the target was hit
            animationSystem.Play(target.Body, targetAnimation, targetPlacement);
            Land(arte, attacker, target, revives);
        }
    }

    private void Land(Arte arte, AttackerSnapshot attacker, BattleFighter target, bool revives)
    {
        // a revive brings the fighter back before anything else happens (remove states comes first)
        if (target.IsDown && revives) GetUp(target);

        var outcome = hitCalculator.Resolve(arte, attacker, target);
        if (outcome.Missed)
        {
            battleHud.Popup("Miss", Head(target), HudPopupKind.Miss);
            return;
        }

        var stats = target.Stats;
        var before = stats.Life;
        stats.Life = Math.Clamp(stats.Life - outcome.LifeChange, 0, stats.MaxLife);
        var change = before - stats.Life;

        // Threat: all the damage dealt, half of the healing actually done (overheal doesn't count)
        attacker.Source.Threat += change > 0 ? change : -change * 0.5;

        if (change >= 0) battleHud.Popup(change.ToString(), Head(target), outcome.Critical ? HudPopupKind.Critical : HudPopupKind.Damage);
        else battleHud.Popup((-change).ToString(), Head(target), HudPopupKind.Heal);

        if (stats.Life <= 0 && !target.IsDown) Fall(target);
    }

    // Incapacitated: every state goes (states come later), it can't move, nobody bumps into it, and enemies fade away
    private void Fall(BattleFighter fighter)
    {
        fighter.IsDown = true;
        fighter.Casting = null;
        fighter.Threat = 0;
        Lock(fighter, true);
        Solid(fighter, false);
        movementRouteSystem.FadeTo(fighter.Body, fighter.IsEnemy ? 0 : 0.4, 400);
    }

    private void GetUp(BattleFighter fighter)
    {
        fighter.IsDown = false;
        Lock(fighter, false);
        Solid(fighter, true);
        movementRouteSystem.FadeTo(fighter.Body, 1, 200);
    }

    // victory when every enemy is down, defeat when every party member is (no game over screen yet, it just ends)
    private void CheckForEnd(ActiveBattle battle)
    {
        var enemiesDown = battle.Fighters.Where(x => x.OriginalTeam == BattleFighter.EnemyTeam).All(x => x.IsDown);
        var partyDown = battle.Fighters.Where(x => x.OriginalTeam == BattleFighter.PartyTeam).All(x => x.IsDown);
        if (!enemiesDown && !partyDown)
        {
            _endTimer = 0;
            return;
        }

        if (++_endTimer >= EndDelay) End(battle);
    }

    // the battle officially ends: the fallen get up with 1 Life, and the party goes back to the story map
    private void End(ActiveBattle battle)
    {
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

    // who an arte's scope lets it touch: teams are from the user's side, and a target is judged by its Original Team
    private static bool InScope(EffectScope scope, BattleFighter user, BattleFighter target, bool revives)
    {
        var ally = target.OriginalTeam == user.Team;
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

    // the arte's hitboxes in scaled map pixels, drawn from the tile the user stands on. An arte that turns with its user
    // uses the hitboxes drawn for the way they face, exactly as drawn. A direction with none of its own turns the down
    // hitboxes about the tile's middle instead, and facing up those are measured from the top of the user's hitbox,
    // so an up swing touches the body instead of leaving a gap above it
    private IEnumerable<Rect> Hitboxes(BattleFighter user, Arte arte, Direction facing)
    {
        var scale = gameDataService.ScaleFactor;
        var feet = FeetTile(user.Body);
        var turn = arte.DirectionRotate ? facing : Direction.Down;

        var drawn = arte.HitboxesFor(turn);
        if (turn == Direction.Down || drawn.Count > 0)
        {
            foreach (var box in drawn) yield return new Rect(feet.X + (box.OffsetX * scale), feet.Y + (box.OffsetY * scale), box.Width * scale, box.Height * scale);
            yield break;
        }

        var pushY = turn == Direction.Up && hitboxSystem.Main(user.Body) is { } body ? body.Y - feet.Y : 0.0;
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

    private static Direction Facing(BattleFighter fighter)
    {
        return fighter.Body.GetModule<PositionModule>() is { } position ? (Direction) (Math.Max(0, (int) position.Direction) % 4) : Direction.Down;
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

    private Guid Incapacitated() => gameDataService.States.FirstOrDefault(x => x.Name == IncapacitatedName)?.Guid ?? Guid.Empty;

    #endregion
}
