using System.Collections.ObjectModel;
using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameLibrary.Services.GameData;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameEngine.Interface;

/// <summary>
/// The battle HUD, the screen layer shown while a battle is on. It never pauses the game. The party's panels run along
/// the bottom (face, name, Life and Mana), a boss gets one big bar across the top, and the other enemies get a small
/// Life bar over their heads, but only once the player has inspected that enemy.
/// The game loop reads the battle every update and the HUD changes on the ui thread.
/// </summary>
public partial class BattleHud(
    IGameStateService gameStateService,
    IGameDataService gameDataService,
    InterfaceImages images) : ScreenLayer
{
    #region Constants

    // TEMP: every enemy's bar shows until the bestiary exists and enemies can be inspected
    private const bool ShowAllEnemyBars = true;

    // the small bar over an enemy's head, in scaled map pixels
    public const double EnemyBarWidth = 40;

    private const double EnemyBarGap = 6;

    // the arte keys' labels, in slot order (the same keys the player controller reads)
    private static readonly string[] SlotLabels = ["1", "2", "3", "4", "5", "6", "Q", "W", "E", "R", "T", "Y", "A", "S", "D", "F", "G", "H"];

    // the item keys' labels, in slot order (the same keys the player controller reads)
    private static readonly string[] ItemLabels = ["Z", "X", "C", "V", "B"];

    // the cast bar over the leader's head, in scaled map pixels
    public const double CastBarWidth = 48;

    // the bar over the leader's head while running away
    public const double FleeBarWidth = 64;

    #endregion

    #region Fields

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private ActiveBattle? _shown;

    // the rows for the battle that's shown, in the battle's fighter order, made on the ui thread
    private (BattleFighter fighter, HudFighter row)[] _rows = [];

    // every state's icon: its sheet and the cell to show, made on the ui thread when a battle opens
    private Dictionary<Guid, (ImageSource sheet, Rect cell)> _stateIcons = [];

    #endregion

    #region Properties

    public override bool PausesGame => false;

    public ObservableCollection<HudFighter> Party { get; } = [];

    public ObservableCollection<HudFighter> Enemies { get; } = [];

    public HudFighter? Boss
    {
        get;
        private set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasBoss));
        }
    }

    public bool HasBoss => Boss is not null;

    // the leader's arte keys, in the same rows as the keyboard: 1-6, Q-Y, A-H
    public ObservableCollection<HudArteSlot> Slots { get; } = [.. SlotLabels.Select(x => new HudArteSlot(x))];

    public IEnumerable<HudArteSlot> SlotRow1 => Slots.Take(6);

    public IEnumerable<HudArteSlot> SlotRow2 => Slots.Skip(6).Take(6);

    public IEnumerable<HudArteSlot> SlotRow3 => Slots.Skip(12).Take(6);

    // the party's item keys, each showing its item, how many are left, and the shared item cooldown
    public ObservableCollection<HudArteSlot> ItemSlots { get; } = [.. ItemLabels.Select(x => new HudArteSlot(x))];

    // the numbers floating up over the fighters
    public ObservableCollection<HudPopup> Popups { get; } = [];

    // the leader's cast: how far along it is (0 to 1), over the leader's head
    public bool IsCasting
    {
        get;
        private set => SetField(ref field, value);
    }

    public double CastFraction
    {
        get;
        private set => SetField(ref field, value);
    }

    public double CastX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double CastY
    {
        get;
        private set => SetField(ref field, value);
    }

    // running away: how far along it is (0 to 1), over the leader's head
    public bool IsFleeing
    {
        get;
        private set => SetField(ref field, value);
    }

    public double FleeFraction
    {
        get;
        private set => SetField(ref field, value);
    }

    public double FleeX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double FleeY
    {
        get;
        private set => SetField(ref field, value);
    }

    // TEST: what the state test keys last did (BattleStateTester), empty until one is pressed
    public string TestText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    // moves the layer of enemy bars with the map, the same whole pixels the renderer moves the pictures by
    public double OffsetX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double OffsetY
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    public override void Update(GameCamera camera)
    {
        var battle = gameStateService.Battle is { IsEnding: false, Fighters.Count: > 0 } current ? current : null;
        if (battle != _shown)
        {
            _shown = battle;
            if (battle is null) _dispatcherQueue.TryEnqueue(Close);
            else _ = Open(battle);
            return;
        }

        if (battle is null) return;

        // read on the game loop, shown on the ui thread
        var scale = gameDataService.ScaleFactor;
        var values = battle.Fighters.Select(x => (x.Stats.Life, x.Stats.MaxLife, x.Stats.Mana, x.Stats.MaxMana, Bar: BarSpot(x.Body, scale), States: StateValues(x))).ToArray();
        var offsetX = -Math.Round(camera.X);
        var offsetY = -Math.Round(camera.Y);
        var leader = battle.Fighters.FirstOrDefault(x => x.IsLeader);
        var slots = SlotValues(leader);
        var items = ItemValues(battle);
        var cast = leader is { Casting: not null, CastTotal: > 0 } caster
            ? ((double?) (1.0 - (caster.CastLeft / (double) caster.CastTotal)), BarSpot(caster.Body, scale))
            : (null, default);
        var flee = leader is not null && battle.FleeFraction > 0 ? ((double?) battle.FleeFraction, BarSpot(leader.Body, scale)) : (null, default);
        _dispatcherQueue.TryEnqueue(() =>
        {
            Show(battle, values, offsetX, offsetY);
            ShowSlots(slots, cast.Item1, cast.Item2);
            ShowItems(items);
            ShowFlee(flee.Item1, flee.Item2);
            StepPopups();
        });
    }

    // a number over a fighter's head (scaled map pixels), called from the game loop
    public void Popup(string text, Point at, HudPopupKind kind)
    {
        _dispatcherQueue.TryEnqueue(() => Popups.Add(new HudPopup(text, at.X, at.Y, kind)));
    }

    // TEST: the state test keys' line, called from the game loop
    public void ShowTest(string text) => _dispatcherQueue.TryEnqueue(() => TestText = text);

    #endregion

    #region Private Methods

    // the faces and arte icons are measured off the ui thread, then the rows are made on it
    private async Task Open(ActiveBattle battle)
    {
        var faces = new List<(string path, Rect cell)?>();
        foreach (var fighter in battle.Fighters) faces.Add(fighter.IsEnemy ? null : await images.FaceCell(fighter.Stats.FaceName, fighter.Stats.FaceIndex));

        var leaderSlots = battle.Fighters.FirstOrDefault(x => x.IsLeader)?.Slots ?? [];
        var itemIcons = new List<(string name, (string path, Rect cell)? icon)?>();
        foreach (var item in battle.ItemSlots) itemIcons.Add(item is null ? null : (item.Name, await images.IconCell(item.Icon)));

        var icons = new List<(string name, string cost, (string path, Rect cell)? icon)?>();
        for (var i = 0; i < Slots.Count; i++)
        {
            if (leaderSlots.ElementAtOrDefault(i) is not { } arte)
            {
                icons.Add(null);
                continue;
            }

            icons.Add((arte.Name, arte.Cost > 0 ? arte.Cost.ToString() : string.Empty, await images.IconCell(arte.Icon)));
        }

        var stateCells = new List<(Guid id, string path, Rect cell)>();
        foreach (var state in gameDataService.States.Where(x => !x.Hidden).ToArray())
        {
            if (await images.IconCell(state.Icon) is { } icon) stateCells.Add((state.Guid, icon.path, icon.cell));
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_shown != battle) return;

            // the states share a picture per sheet
            var sheets = stateCells.Select(x => x.path).Distinct().ToDictionary(x => x, x => new BitmapImage(new Uri(x)));
            _stateIcons = stateCells.ToDictionary(x => x.id, x => ((ImageSource) sheets[x.path], x.cell));

            Party.Clear();
            Enemies.Clear();
            Boss = null;
            var rows = new List<(BattleFighter, HudFighter)>();
            for (var i = 0; i < battle.Fighters.Count; i++)
            {
                var fighter = battle.Fighters[i];
                var row = new HudFighter(fighter.Stats.Name, fighter.IsLeader);
                if (faces[i] is { } face)
                {
                    row.Face = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(face.path));
                    row.FaceRect = face.cell;
                }

                if (!fighter.IsEnemy) Party.Add(row);
                else if (fighter.IsBoss && Boss is null) Boss = row;
                else
                {
                    row.ShowsBar = ShowAllEnemyBars || gameStateService.InspectedEnemies.Contains(fighter.Stats.Guid);
                    Enemies.Add(row);
                }

                rows.Add((fighter, row));
            }

            _rows = [.. rows];

            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                slot.HasArte = icons[i] is not null;
                slot.Name = icons[i]?.name ?? string.Empty;
                slot.CostText = icons[i]?.cost ?? string.Empty;
                slot.Icon = icons[i]?.icon is { } icon ? new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(icon.path)) : null;
                slot.IconRect = icons[i]?.icon?.cell ?? default;
            }

            for (var i = 0; i < ItemSlots.Count; i++)
            {
                var slot = ItemSlots[i];
                slot.HasArte = itemIcons[i] is not null;
                slot.Name = itemIcons[i]?.name ?? string.Empty;
                slot.Icon = itemIcons[i]?.icon is { } icon ? new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(icon.path)) : null;
                slot.IconRect = itemIcons[i]?.icon?.cell ?? default;
            }

            IsOpen = true;
        });
    }

    private void Close()
    {
        _rows = [];
        Popups.Clear();
        TestText = string.Empty;
        IsCasting = false;
        IsFleeing = false;
        foreach (var slot in Slots) slot.HasArte = false;
        foreach (var slot in ItemSlots) slot.HasArte = false;
        Party.Clear();
        Enemies.Clear();
        Boss = null;
        IsOpen = false;
    }

    private void Show(ActiveBattle battle, (int life, int maxLife, int mana, int maxMana, Point bar, HudStateValue[] states)[] values, double offsetX, double offsetY)
    {
        if (_shown != battle || _rows.Length != values.Length) return;

        OffsetX = offsetX;
        OffsetY = offsetY;
        for (var i = 0; i < values.Length; i++)
        {
            var (life, maxLife, mana, maxMana, bar, states) = values[i];
            _rows[i].row.Set(life, maxLife, mana, maxMana);
            _rows[i].row.MoveBar(bar.X, bar.Y);
            _rows[i].row.SetStates(states, CreateState);
        }
    }

    // the states a fighter shows, read on the game loop. Hidden ones never do
    private static HudStateValue[] StateValues(BattleFighter fighter)
    {
        return
        [
            .. fighter.States.Where(x => !x.State.Hidden).Select(x =>
            {
                var total = BattleState.Frames(x.State);
                return new HudStateValue(x.State.Guid, total > 0 ? Math.Clamp(x.Left / (double) total, 0, 1) : 0, x.Stacks);
            })
        ];
    }

    private HudState CreateState(Guid id)
    {
        var state = new HudState(id) { HasArte = true };
        if (!_stateIcons.TryGetValue(id, out var icon)) return state;

        state.Icon = icon.sheet;
        state.IconRect = icon.cell;
        return state;
    }

    // each slot: how much of its cooldown is left (0 ready), short of Mana, being cast
    private static (double fraction, bool noMana, bool casting)[] SlotValues(BattleFighter? leader)
    {
        var values = new (double, bool, bool)[SlotLabels.Length];
        if (leader is null) return values;

        for (var i = 0; i < values.Length; i++)
        {
            if (leader.Slots.ElementAtOrDefault(i) is not { } arte) continue;

            var fraction = leader.Cooldowns.TryGetValue(arte.Guid, out var cooldown) && cooldown.Total > 0 ? cooldown.Left / (double) cooldown.Total : 0;
            values[i] = (fraction, leader.Stats.Mana < arte.Cost, leader.Casting == arte);
        }

        return values;
    }

    // each item key: whether it holds an item, how many are left in the bag, and how much of the shared cooldown is left
    private (bool has, int count, double fraction)[] ItemValues(ActiveBattle battle)
    {
        var fraction = battle.ItemCooldownTotal > 0 ? battle.ItemCooldown / (double) battle.ItemCooldownTotal : 0;
        return [.. battle.ItemSlots.Select(x => x is null ? (false, 0, 0.0) : (true, gameStateService.Items.GetValueOrDefault(x.Guid), fraction))];
    }

    private void ShowItems((bool has, int count, double fraction)[] values)
    {
        for (var i = 0; i < ItemSlots.Count && i < values.Length; i++)
        {
            var slot = ItemSlots[i];
            slot.HasArte = values[i].has;
            slot.CostText = values[i].has ? values[i].count.ToString() : string.Empty;
            slot.Set(values[i].fraction, false, false);
        }
    }

    private void ShowSlots((double fraction, bool noMana, bool casting)[] values, double? cast, Point castAt)
    {
        for (var i = 0; i < Slots.Count && i < values.Length; i++) Slots[i].Set(values[i].fraction, values[i].noMana, values[i].casting);

        IsCasting = cast is not null;
        if (cast is not { } fraction) return;

        CastFraction = fraction;
        CastX = castAt.X + (EnemyBarWidth / 2.0) - (CastBarWidth / 2.0);
        CastY = castAt.Y - 8;
    }

    private void ShowFlee(double? fraction, Point at)
    {
        IsFleeing = fraction is not null;
        if (fraction is not { } value) return;

        FleeFraction = value;
        FleeX = at.X + (EnemyBarWidth / 2.0) - (FleeBarWidth / 2.0);
        FleeY = at.Y - 20;
    }

    private void StepPopups()
    {
        foreach (var popup in Popups.ToArray())
        {
            popup.Step();
            if (popup.IsDone) Popups.Remove(popup);
        }
    }

    // just over the head: centred on the sprite, a little above its top
    private static Point BarSpot(MapEntity body, double scale)
    {
        if (body.State<CharacterImageState>().Sprite is not { } sprite) return default;

        return new Point(sprite.X + (sprite.Width * scale / 2.0) - (EnemyBarWidth / 2.0), sprite.Y - EnemyBarGap);
    }

    #endregion
}
