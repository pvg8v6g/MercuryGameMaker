using Windows.Foundation;
using GameEngine.Models;
using GameLibrary.Models;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Battle;

/// <summary>
/// What a fallen enemy leaves on the battle map: its money and whichever of its drops came up. Each one is an icon
/// lying where the enemy fell, drawn from the icon sheet's No Background twin (the framed icon only when there's no
/// twin), and it's picked up by a party member walking over it. Anything not picked up by the time the battle ends is lost.
/// </summary>
public class BattleDrops(IGameDataService gameDataService, IGraphicsService graphicsService)
{
    #region Constants

    // every icon sheet is this many icons across (Graphics\Icons\README.md)
    private const int SheetColumns = 16;

    // how far a pickup bobs, in unscaled pixels, and how fast
    private const double BobHeight = 2;
    private const double BobSpeed = 0.12;

    // several pickups from one enemy are spread around where it fell, in tiles
    private const double SpreadTiles = 0.45;

    #endregion

    #region Fields

    private readonly List<Pickup> _pickups = [];
    private readonly Random _random = new();

    #endregion

    #region Properties

    public int Count => _pickups.Count;

    // the pictures to draw this update
    public IEnumerable<SceneSprite> Sprites => _pickups.Select(x => x.Sprite).OfType<SceneSprite>().ToArray();

    #endregion

    #region Methods

    // the sheets the battle's drops are drawn from, so the map can preload them with its other images
    public IEnumerable<string> SheetPaths()
    {
        var icons = gameDataService.Enemies
            .SelectMany(x => x.Drops)
            .Select(x => AllItems().FirstOrDefault(item => item.Guid == x.DropId)?.Icon)
            .OfType<GameIcon>()
            .Append(gameDataService.GameSettings.MoneyIcon)
            .Where(x => !x.IsNone);

        return icons.Select(x => graphicsService.GetDropSheetPath(x.Sheet)).Distinct().Where(File.Exists);
    }

    /// <summary>
    /// The enemy fell with its feet at this point (map pixels): its money and the drops that came up are laid down around it.
    /// </summary>
    public void Spawn(Fighter enemy, Point feet, int z)
    {
        var found = new List<Pickup>();
        if (enemy.MonetaryValue > 0) found.Add(new Pickup(null, enemy.MonetaryValue, gameDataService.GameSettings.MoneyIcon, $"{enemy.MonetaryValue} {gameDataService.GameSettings.MoneyName}"));

        var items = AllItems();
        foreach (var drop in enemy.Drops)
        {
            if (_random.Next(100) >= drop.DropPercent || items.FirstOrDefault(x => x.Guid == drop.DropId) is not { } item) continue;

            found.Add(new Pickup(item.Guid, 1, item.Icon, item.Name));
        }

        var scale = gameDataService.ScaleFactor;
        var tile = gameDataService.GridSize * scale;
        for (var i = 0; i < found.Count; i++)
        {
            var pickup = found[i];
            var angle = (Math.PI * 2 * i / found.Count) + (Math.PI / 4);
            var spread = found.Count > 1 ? tile * SpreadTiles : 0;
            pickup.Center = new Point(feet.X + (Math.Cos(angle) * spread), feet.Y + (Math.Sin(angle) * spread));
            pickup.Sprite = NewSprite(pickup, z);
            _pickups.Add(pickup);
        }
    }

    /// <summary>
    /// One update: pickups bob a little, and any a collector's feet are over (within reach, map pixels) are taken. Returns
    /// each pickup that was taken with the index of whoever took it.
    /// </summary>
    public List<(Pickup pickup, int collector)> Update(IReadOnlyList<Point> collectors, double reach)
    {
        var taken = new List<(Pickup, int)>();
        var scale = gameDataService.ScaleFactor;
        foreach (var pickup in _pickups.ToArray())
        {
            pickup.Age++;
            if (pickup.Sprite is { } sprite) sprite.Y = pickup.Center.Y - (sprite.Height * scale / 2.0) - ((1 + Math.Sin(pickup.Age * BobSpeed)) * BobHeight * scale);

            for (var i = 0; i < collectors.Count; i++)
            {
                var dx = collectors[i].X - pickup.Center.X;
                var dy = collectors[i].Y - pickup.Center.Y;
                if ((dx * dx) + (dy * dy) > reach * reach) continue;

                taken.Add((pickup, i));
                _pickups.Remove(pickup);
                break;
            }
        }

        return taken;
    }

    // the battle is over: whatever's left is lost
    public void Clear() => _pickups.Clear();

    #endregion

    #region Private Methods

    private IEnumerable<Item> AllItems()
    {
        return gameDataService.Consumables.Cast<Item>()
            .Concat(gameDataService.Ingredients)
            .Concat(gameDataService.Materials)
            .Concat(gameDataService.Equipment);
    }

    // the icon's cell on the No Background sheet, at the picture's own size standing on its spot. No icon, no picture
    private SceneSprite? NewSprite(Pickup pickup, int z)
    {
        if (pickup.Icon.IsNone) return null;

        var path = graphicsService.GetDropSheetPath(pickup.Icon.Sheet);
        if (!File.Exists(path)) return null;

        var size = graphicsService.IconSize;
        var scale = (float) gameDataService.ScaleFactor;
        var picture = new PictureBox
        {
            ImagePath = path,
            Z = z,
            ScaleX = scale,
            ScaleY = scale,
            Viewport = new Rect((pickup.Icon.Index % SheetColumns) * size, (pickup.Icon.Index / SheetColumns) * size, size, size)
        };

        return new SceneSprite(picture)
        {
            Width = size,
            Height = size,
            X = pickup.Center.X - (size * scale / 2.0),
            Y = pickup.Center.Y - (size * scale / 2.0),
            SortY = pickup.Center.Y - 1
        };
    }

    #endregion

    #region Types

    // what's lying on the map: money (no item), or one item
    public class Pickup(Guid? itemId, int amount, GameIcon icon, string name)
    {
        public Guid? ItemId => itemId;

        public int Amount => amount;

        public GameIcon Icon => icon;

        public string Name => name;

        public Point Center { get; set; }

        public SceneSprite? Sprite { get; set; }

        public int Age { get; set; }
    }

    #endregion
}
