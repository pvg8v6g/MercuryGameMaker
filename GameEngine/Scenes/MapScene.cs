using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Scenes;

/// <summary>
/// A running map. Loading copies the map's entities so playing never changes the saved game,
/// every update runs the systems in order, moves the camera and renders the map images and characters.
/// The canvas updates on its own thread, so a load waits for the update in progress and the other way round.
/// </summary>
public class MapScene(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    IJsonService jsonService,
    IEnumerable<IEntitySystem> systems,
    HitboxSystem hitboxSystem,
    PlayerSpawner playerSpawner,
    MapAudio mapAudio,
    DebugOverlay debugOverlay,
    Renderer renderer)
{
    #region Constants

    // updates the screen takes to fade out, and again to fade back in (15 is a quarter second)
    private const int FadeUpdates = 15;

    #endregion

    #region Fields

    private readonly Lock _lock = new();
    private readonly IEntitySystem[] _systems = [.. systems];
    private readonly List<SceneSprite> _mapImages = [];
    private readonly List<EnvironmentAnimation> _environments = [];

    // changing maps: fade out, load the map while the screen is black, fade back in
    private TransferPhase _phase;
    private EdgeTransfer? _transfer;
    private int _fadeTimer;

    #endregion

    #region Properties

    public GameCamera Camera { get; } = new();

    // what the canvas draws
    public Renderer Renderer => renderer;

    public DebugOverlay DebugOverlay => debugOverlay;

    // the map's audio fades along with the screen
    private double FadeSeconds => FadeUpdates * gameDataService.FrameRate / 1000.0;

    #endregion

    #region Load

    // the party leader appears on the spawn tile, or exactly at a spot (unscaled map pixels) when one is given
    public async Task Load(GameMap map, SpawnPoint spawn, Point? at = null)
    {
        lock (_lock) gameStateService.IsTransferring = true;

        var entities = map.Entities.Select(x => new MapEntity(jsonService.Copy(x)!)).ToList();
        var player = playerSpawner.Create(spawn, at) is { } leader ? new MapEntity(leader) : null;
        if (player is not null) entities.Add(player);
        foreach (var entity in entities) ScalePositions(entity);

        var mapImages = await LoadMapImages(map);
        var environments = await LoadEnvironments(map);
        renderer.Preload(mapImages.Concat(environments.SelectMany(x => x.Frames)).Select(x => x.Picture.ImagePath));
        foreach (var system in _systems) await system.Load(entities);

        lock (_lock)
        {
            gameStateService.ActiveMap = map;
            gameStateService.Player = player;
            gameStateService.MapEntities.Clear();
            gameStateService.MapEntities.AddRange(entities);
            gameStateService.MapAreas.Clear();
            gameStateService.MapAreas.AddRange(map.Areas);
            _mapImages.Clear();
            _mapImages.AddRange(mapImages);
            _environments.Clear();
            _environments.AddRange(environments);
            Camera.Follow(null);

            // a transfer stays paused until the new map has faded in, the first map of the game starts straight away
            if (_phase == TransferPhase.Loading) _phase = TransferPhase.FadingIn;
            else gameStateService.IsTransferring = false;
        }

        // not waited for, the map never holds up on a track loading
        _ = mapAudio.Play(map, FadeSeconds);
    }

    // the editor saves positions in unscaled map pixels, the game plays in scaled ones
    private void ScalePositions(MapEntity entity)
    {
        foreach (var position in entity.Entity.Pages.SelectMany(x => x.Modules).OfType<PositionModule>())
        {
            position.X *= gameDataService.ScaleFactor;
            position.Y *= gameDataService.ScaleFactor;
        }
    }

    // a layer is drawn when its image exists for the map id, the map's layer options change its defaults
    private async Task<List<SceneSprite>> LoadMapImages(GameMap map)
    {
        var scale = (float) gameDataService.ScaleFactor;
        var images = new List<SceneSprite>();
        foreach (var type in Enum.GetValues<MapLayerType>())
        {
            var path = new MapLayer(type).ImagePath(locationService.GraphicsDirectory!, map.Id);
            if (!File.Exists(path)) continue;

            var defaults = MapLayer.Defaults(type);
            var option = map.LayerOptions.FirstOrDefault(x => x.Type == type);
            var (width, height) = await graphicsService.GetImageDimensions(path);
            images.Add(new SceneSprite(new PictureBox
            {
                ImagePath = path,
                Z = (int) (option?.Z ?? defaults.Z),
                Opacity = (float) (option?.Opacity ?? defaults.Opacity),
                BlendMode = option is null ? defaults.MapBlendMode : option.MapBlendMode,
                ScaleX = scale,
                ScaleY = scale
            })
            {
                Width = width,
                Height = height
            });
        }

        return images;
    }

    // every frame of every environment the map turned on, each frame tiled until it covers the map
    private async Task<List<EnvironmentAnimation>> LoadEnvironments(GameMap map)
    {
        var scale = gameDataService.ScaleFactor;
        var mapWidth = map.Width * gameDataService.GridSize * scale;
        var mapHeight = map.Height * gameDataService.GridSize * scale;
        var animations = new List<EnvironmentAnimation>();
        foreach (var environment in map.Environments)
        {
            var paths = environment.FramePaths(locationService.GraphicsDirectory!);
            if (paths.Length == 0) continue;

            var (width, height) = await graphicsService.GetImageDimensions(paths[0]);
            if (width <= 0 || height <= 0) continue;

            var frames = paths.Select(path => Tiles(environment, path, width, height)).ToArray();
            animations.Add(new EnvironmentAnimation(frames));
        }

        return animations;

        SceneSprite[] Tiles(MapEnvironment environment, string path, double width, double height)
        {
            var tiles = new List<SceneSprite>();
            for (var y = 0.0; y < mapHeight; y += height * scale)
            {
                for (var x = 0.0; x < mapWidth; x += width * scale)
                {
                    tiles.Add(new SceneSprite(new PictureBox
                    {
                        ImagePath = path,
                        Z = (int) environment.Z,
                        Opacity = (float) environment.Opacity,
                        BlendMode = environment.MapBlendMode,
                        ScaleX = (float) scale,
                        ScaleY = (float) scale
                    })
                    {
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height
                    });
                }
            }

            return [.. tiles];
        }
    }

    #endregion

    #region Update

    // while transferring the map is paused: it is still drawn, but only the systems that draw it run
    public void Update()
    {
        EdgeTransfer? transfer;
        lock (_lock)
        {
            transfer = UpdateFade();
            if (gameStateService.ActiveMap is { } map) UpdateMap(map, gameStateService.IsTransferring);
        }

        // loaded outside the lock, the load takes the lock itself once the map is ready
        if (transfer is not null) _ = Transfer(transfer);
    }

    private void UpdateMap(GameMap map, bool paused)
    {
        foreach (var system in _systems.Where(x => !paused || x.RunsWhilePaused))
        {
            foreach (var entity in gameStateService.MapEntities) system.Update(entity);
        }

        gameStateService.Player ??= gameStateService.MapEntities.FirstOrDefault(x => x.HasModule<InputModule>());
        UpdateCamera(map);
        if (!paused)
        {
            foreach (var environment in _environments) environment.Update();
        }

        renderer.Render(_mapImages.Concat(_environments.SelectMany(x => x.Current)).Concat(CharacterSprites()), Camera);
        debugOverlay.Update(map, Camera);
        if (paused) return;

        if (inputService.Lag > 0) inputService.Lag--;

        // the map pauses (enemies, arrows, the player) for the whole transfer: fade out, load, fade in
        if (gameStateService.EdgeTransfer is not { } requested) return;
        gameStateService.EdgeTransfer = null;
        gameStateService.IsTransferring = true;
        _transfer = requested;
        _phase = TransferPhase.FadingOut;
        _fadeTimer = 0;
        mapAudio.Leave(requested.Map, FadeSeconds);
    }

    // moves the fade along, gives back the transfer to load once the screen is fully black
    private EdgeTransfer? UpdateFade()
    {
        switch (_phase)
        {
            case TransferPhase.FadingOut:
                renderer.SetFade((double) ++_fadeTimer / FadeUpdates);
                if (_fadeTimer < FadeUpdates) return null;

                var transfer = _transfer;
                _transfer = null;
                _phase = TransferPhase.Loading;
                return transfer;

            // the new map is drawn paused while it fades in, then everything carries on
            case TransferPhase.FadingIn:
                renderer.SetFade((double) --_fadeTimer / FadeUpdates);
                if (_fadeTimer > 0) return null;

                _phase = TransferPhase.None;
                gameStateService.IsTransferring = false;
                return null;

            default:
                return null;
        }
    }

    // a map that fails to load fades back in on the map the player was on instead of freezing the game
    private async Task Transfer(EdgeTransfer transfer)
    {
        try
        {
            await Load(transfer.Map, new SpawnPoint { Direction = transfer.Direction }, new Point(transfer.X, transfer.Y));
        }
        catch
        {
            lock (_lock) _phase = TransferPhase.FadingIn;

            // the map's audio already started fading out, it comes back with the map
            if (gameStateService.ActiveMap is { } map) _ = mapAudio.Play(map, FadeSeconds);
        }
    }

    private void UpdateCamera(GameMap map)
    {
        if (Camera.Target is null && Camera.Point is null) Camera.Follow(gameStateService.Player);
        if ((Camera.Point ?? Center(Camera.Target)) is not { } center) return;

        var scale = gameDataService.GridSize * gameDataService.ScaleFactor;
        var view = new Size(gameDataService.GameWidth, gameDataService.GameHeight);
        Camera.Update(center, view, new Size(map.Width * scale, map.Height * scale));
    }

    // the middle of the entity's main hitbox, or of the tile it stands on when it has none
    private Point? Center(MapEntity? entity)
    {
        if (entity?.GetModule<PositionModule>() is not { } position) return null;
        if (hitboxSystem.Main(entity) is { } hitbox) return new Point(hitbox.X + (hitbox.Width / 2.0), hitbox.Y + (hitbox.Height / 2.0));

        var tile = gameDataService.GridSize * gameDataService.ScaleFactor;
        return new Point(position.X + (tile / 2.0), position.Y + (tile / 2.0));
    }

    private IEnumerable<SceneSprite> CharacterSprites()
    {
        return gameStateService.MapEntities
            .Where(x => x.HasModule<CharacterImageModule>())
            .Select(x => x.State<CharacterImageState>().Sprite)
            .OfType<SceneSprite>();
    }

    #endregion

    #region Types

    private enum TransferPhase
    {
        None,
        FadingOut,
        Loading,
        FadingIn
    }

    #endregion
}
