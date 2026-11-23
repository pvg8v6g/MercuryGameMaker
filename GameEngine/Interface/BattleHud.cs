using System.Collections.ObjectModel;
using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using Microsoft.UI.Dispatching;

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
    IGraphicsService graphicsService,
    ILocationService locationService) : ScreenLayer
{
    #region Constants

    // TEMP: every enemy's bar shows until the bestiary exists and enemies can be inspected
    private const bool ShowAllEnemyBars = true;

    // the small bar over an enemy's head, in scaled map pixels
    public const double EnemyBarWidth = 40;

    private const double EnemyBarGap = 6;

    // the arte keys' labels, in slot order (the same keys the player controller reads)
    private static readonly string[] SlotLabels = ["1", "2", "3", "4", "5", "6", "Q", "W", "E", "R", "T", "Y", "A", "S", "D", "F", "G", "H"];

    // the cast bar over the leader's head, in scaled map pixels
    public const double CastBarWidth = 48;

    #endregion

    #region Fields

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private ActiveBattle? _shown;

    // the rows for the battle that's shown, in the battle's fighter order, made on the ui thread
    private (BattleFighter fighter, HudFighter row)[] _rows = [];

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
        var values = battle.Fighters.Select(x => (x.Stats.Life, x.Stats.MaxLife, x.Stats.Mana, x.Stats.MaxMana, Bar: BarSpot(x.Body, scale))).ToArray();
        var offsetX = -Math.Round(camera.X);
        var offsetY = -Math.Round(camera.Y);
        var leader = battle.Fighters.FirstOrDefault(x => x.IsLeader);
        var slots = SlotValues(leader);
        var cast = leader is { Casting: not null, CastTotal: > 0 } caster
            ? ((double?) (1.0 - (caster.CastLeft / (double) caster.CastTotal)), BarSpot(caster.Body, scale))
            : (null, default);
        _dispatcherQueue.TryEnqueue(() =>
        {
            Show(battle, values, offsetX, offsetY);
            ShowSlots(slots, cast.Item1, cast.Item2);
            StepPopups();
        });
    }

    // a number over a fighter's head (scaled map pixels), called from the game loop
    public void Popup(string text, Point at, HudPopupKind kind)
    {
        _dispatcherQueue.TryEnqueue(() => Popups.Add(new HudPopup(text, at.X, at.Y, kind)));
    }

    #endregion

    #region Private Methods

    // the faces and arte icons are measured off the ui thread, then the rows are made on it
    private async Task Open(ActiveBattle battle)
    {
        var faces = new List<(string path, Rect cell)?>();
        foreach (var fighter in battle.Fighters) faces.Add(fighter.IsEnemy ? null : await FaceCell(fighter));

        var leaderSlots = battle.Fighters.FirstOrDefault(x => x.IsLeader)?.Slots ?? [];
        var icons = new List<(string name, string cost, (string path, Rect cell)? icon)?>();
        for (var i = 0; i < Slots.Count; i++)
        {
            if (leaderSlots.ElementAtOrDefault(i) is not { } arte)
            {
                icons.Add(null);
                continue;
            }

            icons.Add((arte.Name, arte.Cost > 0 ? arte.Cost.ToString() : string.Empty, await IconCell(arte.Icon)));
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_shown != battle) return;

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

            IsOpen = true;
        });
    }

    private void Close()
    {
        _rows = [];
        Popups.Clear();
        IsCasting = false;
        foreach (var slot in Slots) slot.HasArte = false;
        Party.Clear();
        Enemies.Clear();
        Boss = null;
        IsOpen = false;
    }

    private void Show(ActiveBattle battle, (int life, int maxLife, int mana, int maxMana, Point bar)[] values, double offsetX, double offsetY)
    {
        if (_shown != battle || _rows.Length != values.Length) return;

        OffsetX = offsetX;
        OffsetY = offsetY;
        for (var i = 0; i < values.Length; i++)
        {
            var (life, maxLife, mana, maxMana, bar) = values[i];
            _rows[i].row.Set(life, maxLife, mana, maxMana);
            _rows[i].row.MoveBar(bar.X, bar.Y);
        }
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

    private void ShowSlots((double fraction, bool noMana, bool casting)[] values, double? cast, Point castAt)
    {
        for (var i = 0; i < Slots.Count && i < values.Length; i++) Slots[i].Set(values[i].fraction, values[i].noMana, values[i].casting);

        IsCasting = cast is not null;
        if (cast is not { } fraction) return;

        CastFraction = fraction;
        CastX = castAt.X + (EnemyBarWidth / 2.0) - (CastBarWidth / 2.0);
        CastY = castAt.Y - 8;
    }

    private void StepPopups()
    {
        foreach (var popup in Popups.ToArray())
        {
            popup.Step();
            if (popup.IsDone) Popups.Remove(popup);
        }
    }

    // an arte's icon cell on its icon sheet, none when it has no icon
    private async Task<(string path, Rect cell)?> IconCell(GameLibrary.Models.GameIcon icon)
    {
        if (icon.IsNone) return null;

        var path = graphicsService.GetIconSheetPath(icon.Sheet);
        if (!File.Exists(path)) return null;

        var size = graphicsService.IconSize;
        var (width, _) = await graphicsService.GetImageDimensions(path);
        var columns = Math.Max(1, (int) (width / size));
        return (path, new Rect((icon.Index % columns) * size, (icon.Index / columns) * size, size, size));
    }

    // just over the head: centred on the sprite, a little above its top
    private static Point BarSpot(MapEntity body, double scale)
    {
        if (body.State<CharacterImageState>().Sprite is not { } sprite) return default;

        return new Point(sprite.X + (sprite.Width * scale / 2.0) - (EnemyBarWidth / 2.0), sprite.Y - EnemyBarGap);
    }

    // the actor's face cell on its sheet in Graphics\Faces, none when it has no face
    private async Task<(string path, Rect cell)?> FaceCell(BattleFighter fighter)
    {
        if (string.IsNullOrEmpty(fighter.Stats.FaceName) || locationService.GraphicsDirectory is not { } graphics) return null;

        var path = Path.Combine(graphics, "Faces", fighter.Stats.FaceName);
        if (!File.Exists(path)) return null;

        // face sheets are 4 across with any number of rows, so the cells are square (the editor's chooser cuts them
        // the same way)
        var (width, _) = await graphicsService.GetImageDimensions(path);
        var (cellWidth, cellHeight) = await graphicsService.GetSegmentation(path);
        var columns = Math.Max(1, (int) Math.Round(width / cellWidth));
        var index = fighter.Stats.FaceIndex ?? 0;
        var column = index % columns;
        var row = index / columns;
        return (path, new Rect(column * cellWidth, row * cellHeight, cellWidth, cellHeight));
    }

    #endregion
}
