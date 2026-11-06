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
    HitboxSystem hitboxSystem) : PropertyChangedUpdater
{
    #region Fields

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private GameMap? _map;

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
            ShowMapAreas(map, configuration);
        }

        if (!configuration.ShowEntityHitboxes && !configuration.ShowAreas && !configuration.ShowCollisions) return;

        var offsetX = -Math.Round(camera.X);
        var offsetY = -Math.Round(camera.Y);
        var hitboxes = configuration.ShowEntityHitboxes ? hitboxSystem.AllPlaced().Select(ToShape).ToArray() : [];
        _dispatcherQueue.TryEnqueue(() =>
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            ShowHitboxes(hitboxes);
        });
    }

    #endregion

    #region Private Methods

    private void ShowMapAreas(GameMap map, MercuryGameMakerConfiguration configuration)
    {
        var scale = gameDataService.ScaleFactor;
        var shown = map.Areas.Where(x => x.AreaType == MapAreaType.Collision ? configuration.ShowCollisions : configuration.ShowAreas).ToArray();
        var areas = shown.Where(x => x.AreaType != MapAreaType.Collision).Select(x => ToShape(x, scale)).ToArray();
        var collisions = shown.Where(x => x.AreaType == MapAreaType.Collision).Select(x => ToShape(x, scale)).ToArray();
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
        return new DebugShape(area.OffsetX * scale, area.OffsetY * scale, area.Width * scale, area.Height * scale, AreaColor(area.AreaType));
    }

    // the same colors the editor draws them in
    private static Color AreaColor(MapAreaType type) => type switch
    {
        MapAreaType.Collision => ColorHelper.FromArgb(255, 230, 60, 60),
        MapAreaType.Water => ColorHelper.FromArgb(255, 40, 90, 220),
        MapAreaType.Footstep => ColorHelper.FromArgb(255, 80, 200, 110),
        _ => ColorHelper.FromArgb(255, 120, 190, 255)
    };

    #endregion
}
