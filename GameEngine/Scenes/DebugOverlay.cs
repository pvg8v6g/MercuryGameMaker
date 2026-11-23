using GameEngine.Services.GameState;
using GameEngine.Models;
using GameEngine.Services.Start;
using GameEngine.Systems;
using GameLibrary.Configuration;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Location;
using GameLibrary.Utilities;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Windows.UI;

namespace GameEngine.Scenes;

/// <summary>
/// Shows what the game can't otherwise show while testing a map: entity hitboxes, areas and map collisions.
/// What shows is picked in the editor (global settings in config.json) and only applies when testing from the editor.
/// The boxes are in map pixels on a layer the camera moves. Areas never move so they are made once per map,
/// entity hitboxes follow their entities every update.
/// </summary>
public partial class DebugOverlay(
    IGameDataService gameDataService,
    ILocationService locationService,
    IStartService startService,
    IGameStateService gameStateService,
    HitboxSystem hitboxSystem) : PropertyChangedUpdater
{
    #region Fields

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private GameMap? _map;

    // arte hitboxes flashing where an arte landed, each with the updates it has left
    private readonly List<(DebugShape Shape, int Left)> _flashes = [];
    private bool _flashesChanged;

    #endregion

    #region Properties

    public DebugBox[] EntityHitboxes
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public DebugBox[] Areas
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public DebugBox[] Collisions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public DebugBox[] ArteHitboxes
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    // moves the layer of boxes with the map, the same whole pixels the renderer moves the pictures by
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

    // called from the game loop, the boxes change on the ui thread
    public void Update(GameMap map, GameCamera camera)
    {
        if (!startService.IsTest || locationService.Configuration is not { } configuration) return;

        if (map != _map)
        {
            _map = map;
            ShowMapAreas(configuration);
        }

        if (!configuration.ShowEntityHitboxes && !configuration.ShowAreas && !configuration.ShowCollisions && !configuration.ShowArteHitboxes) return;

        var offsetX = -Math.Round(camera.X);
        var offsetY = -Math.Round(camera.Y);
        var hitboxes = configuration.ShowEntityHitboxes ? hitboxSystem.AllPlaced().Select(ToShape).ToArray() : [];
        var flashes = TickFlashes();
        _dispatcherQueue.TryEnqueue(() =>
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            ShowHitboxes(hitboxes);
            if (flashes is not null) ArteHitboxes = [.. flashes.Select(x => new DebugBox(x))];
        });
    }

    // an arte landed: its hitboxes (scaled map pixels) flash for a moment, whether they hit anyone or not.
    // Called from the game loop, only does anything when testing with Show Arte Hitboxes on
    public void FlashArteHitboxes(IEnumerable<(double X, double Y, double Width, double Height)> boxes)
    {
        if (!startService.IsTest || locationService.Configuration?.ShowArteHitboxes != true) return;

        foreach (var box in boxes) _flashes.Add((new DebugShape(box.X, box.Y, box.Width, box.Height, ArteHitboxColor), FlashLength));
        _flashesChanged = true;
    }

    #endregion

    #region Private Methods

    // counts the flashes down. The boxes never move, so the list only goes to the ui when one starts or ends (null otherwise)
    private DebugShape[]? TickFlashes()
    {
        for (var i = _flashes.Count - 1; i >= 0; i--)
        {
            var (shape, left) = _flashes[i];
            if (left > 1)
            {
                _flashes[i] = (shape, left - 1);
                continue;
            }

            _flashes.RemoveAt(i);
            _flashesChanged = true;
        }

        if (!_flashesChanged) return null;

        _flashesChanged = false;
        return [.. _flashes.Select(x => x.Shape)];
    }

    // the map's live areas: the ones painted in the editor plus the footprints of its props
    private void ShowMapAreas(MercuryGameMakerConfiguration configuration)
    {
        var scale = gameDataService.ScaleFactor;
        var shown = gameStateService.MapAreas.Where(x => x.AreaType == MapAreaType.Collision ? configuration.ShowCollisions : configuration.ShowAreas).ToArray();
        var areas = shown.Where(x => x.AreaType != MapAreaType.Collision).Select(x => ToShape(x, scale)).ToArray();
        var collisions = shown.Where(x => x.AreaType == MapAreaType.Collision).SelectMany(x => WithBars(x, scale)).ToArray();
        _dispatcherQueue.TryEnqueue(() =>
        {
            Areas = [.. areas.Select(x => new DebugBox(x))];
            Collisions = [.. collisions.Select(x => new DebugBox(x))];
        });
    }

    // hitboxes mostly just move, the list is only made again when the boxes themselves changed
    private void ShowHitboxes(DebugShape[] shapes)
    {
        if (EntityHitboxes.Length != shapes.Length || EntityHitboxes.Zip(shapes).Any(x => x.First.Color != x.Second.Color))
        {
            EntityHitboxes = [.. shapes.Select(x => new DebugBox(x))];
            return;
        }

        for (var i = 0; i < shapes.Length; i++) EntityHitboxes[i].Move(shapes[i]);
    }

    private static DebugShape ToShape(MapHitbox hitbox)
    {
        return new DebugShape(hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, hitbox.Color.GetHex()?.ToColor() ?? Colors.Red);
    }

    private static DebugShape ToShape(MapArea area, double scale)
    {
        var color = area.Color?.GetHex()?.ToColor() ?? AreaColor(area.AreaType);
        return new DebugShape(area.OffsetX * scale, area.OffsetY * scale, area.Width * scale, area.Height * scale, color);
    }

    // a collision and a bar on each side it's walled off from, like the editor: moving up onto it comes in through its bottom
    private static IEnumerable<DebugShape> WithBars(MapArea area, double scale)
    {
        var box = ToShape(area, scale);
        yield return box;

        var color = AreaColor(MapAreaType.Collision);
        if (area.BlocksUp) yield return new DebugShape(box.X, box.Y + box.Height - BarSize, box.Width, BarSize, color);
        if (area.BlocksDown) yield return new DebugShape(box.X, box.Y, box.Width, BarSize, color);
        if (area.BlocksLeft) yield return new DebugShape(box.X + box.Width - BarSize, box.Y, BarSize, box.Height, color);
        if (area.BlocksRight) yield return new DebugShape(box.X, box.Y, BarSize, box.Height, color);
    }

    // how thick a collision's blocked side is drawn, in scaled map pixels
    private const double BarSize = 6.0;

    // how long an arte's hitboxes flash, in updates (60 = 1 second), and their color (bright yellow, apart from the rest)
    private const int FlashLength = 20;
    private static readonly Color ArteHitboxColor = ColorHelper.FromArgb(255, 255, 220, 40);

    // the same colors the editor draws them in
    private static Color AreaColor(MapAreaType type) => type switch
    {
        MapAreaType.Collision => ColorHelper.FromArgb(255, 230, 60, 60),
        MapAreaType.Water => ColorHelper.FromArgb(255, 40, 90, 220),
        MapAreaType.Footstep => ColorHelper.FromArgb(255, 80, 200, 110),
        MapAreaType.LevelChange => ColorHelper.FromArgb(255, 240, 170, 40),
        MapAreaType.HeatHaze => ColorHelper.FromArgb(255, 240, 90, 30),
        _ => ColorHelper.FromArgb(255, 120, 190, 255)
    };

    #endregion
}
