using Windows.Foundation;
using GameEngine.Battle;
using GameEngine.Interface;
using GameEngine.Models;
using GameEngine.Models.Battle;
using GameEngine.Navigation;
using GameEngine.Pages;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Party;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Areas;
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
    PageRunner pageRunner,
    NavigationService navigationService,
    BalloonSystem balloonSystem,
    AnimationSystem animationSystem,
    GameCamera camera,
    ScreenFade screenFade,
    MapWeather mapWeather,
    PartyService partyService,
    BattleSpawner battleSpawner,
    ScreenLayers screenLayers,
    BattleRunner battleRunner,
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
    private bool _followsLeader;

    // a battle starts loading once the screen is black, like a map change
    private bool _battleWaitingForFade;
    private bool _loadBattle;

    #endregion

    #region Properties

    public GameCamera Camera => camera;

    // what the canvas draws
    public Renderer Renderer => renderer;

    public DebugOverlay DebugOverlay => debugOverlay;

    // the map's audio fades along with the screen
    private double FadeSeconds => FadeUpdates * gameDataService.FrameRate / 1000.0;

    #endregion

    #region Load

    // the party leader appears on the spawn tile, or exactly at a spot (unscaled map pixels) when one is given.
    // A battle also brings its fighters (the rest of the party and the enemies) and everyone's battle walking speed
    public async Task Load(GameMap map, SpawnPoint spawn, Point? at = null, IReadOnlyList<Entity>? fighters = null, int? battleSpeed = null)
    {
        lock (_lock) gameStateService.IsTransferring = true;
        RememberPlaces();
        FollowLeader();

        // the map's own entities, less those a page sent away or put somewhere else, then those a page sent here
        var visitors = gameStateService.VisitingEntities.GetValueOrDefault(map.Guid) ?? [];
        var entities = map.Entities
            .Where(x => !gameStateService.DepartedEntities.Contains((map.Guid, x.Guid)) && visitors.All(visitor => visitor.Entity.Guid != x.Guid))
            .Select(x => new MapEntity(jsonService.Copy(x)!))
            .ToList();
        foreach (var visitor in visitors) entities.Add(await Arrive(visitor));
        var battleBodies = fighters?.Select(x => new MapEntity(x)).ToList() ?? [];
        entities.AddRange(battleBodies);
        foreach (var entity in entities) ScalePositions(entity);
        foreach (var entity in entities) PutBack(map, entity);
        var player = await playerSpawner.Create(spawn, at) is { } leader ? new MapEntity(leader) : null;
        if (player is not null)
        {
            ScalePositions(player);
            entities.Add(player);
        }

        // in battle everyone walks one slower than the player does on the story maps
        if (battleSpeed is { } speed)
        {
            foreach (var body in battleBodies.Append(player).OfType<MapEntity>()) SetSpeed(body, speed);
        }

        // back from a battle: the player walks at exactly the speed saved when it started
        var endedBattle = gameStateService.Battle is { IsEnding: true } ended ? ended : null;
        if (endedBattle is not null && player is not null) SetSpeed(player, endedBattle.StorySpeed);

        var mapImages = await LoadMapImages(map);
        var (propSprites, footprints) = await LoadProps(map);
        mapImages.AddRange(propSprites);
        var environments = await LoadEnvironments(map);
        foreach (var system in _systems) await system.Load(entities);

        // the balloon sheet loads with the map so the first balloon shows straight away
        var preload = mapImages.Concat(environments.SelectMany(x => x.Frames)).Select(x => x.Picture.ImagePath);
        if (balloonSystem.SheetPath is { } balloons) preload = preload.Append(balloons);
        preload = preload.Concat(animationSystem.SheetPaths(entities));
        renderer.Preload(preload);
        mapImages.AddRange(LoadHeatHazes(map));

        lock (_lock)
        {
            gameStateService.ActiveMap = map;
            gameStateService.Player = player;
            if (endedBattle is not null) gameStateService.Battle = null;
            gameStateService.MapEntities.Clear();
            gameStateService.MapEntities.AddRange(entities);
            gameStateService.MapAreas.Clear();
            gameStateService.MapAreas.AddRange(map.Areas);
            gameStateService.MapAreas.AddRange(footprints);

            pageRunner.Clear();

            // a spawn or an edge transfer onto a raised platform starts the player on the platform's level
            if (player is not null) hitboxSystem.StandOnSurface(player);
            _mapImages.Clear();
            _mapImages.AddRange(mapImages);
            _environments.Clear();
            _environments.AddRange(environments);
            Camera.Follow(null);
            mapWeather.Enter(map);

            // a transfer stays paused until the new map has faded in, the first map of the game starts straight away
            if (_phase == TransferPhase.Loading) _phase = TransferPhase.FadingIn;
            else gameStateService.IsTransferring = false;
        }

        // not waited for, the map never holds up on a track loading
        _ = mapAudio.Play(map, FadeSeconds);
    }

    // every walking speed on the entity's pages, so whichever page is active walks at it
    private static void SetSpeed(MapEntity entity, int speed)
    {
        foreach (var movement in entity.Entity.Pages.SelectMany(x => x.Modules).OfType<MovementModule>()) movement.Speed = speed;
    }

    // the player's sprite follows whoever leads the party, listened to once
    private void FollowLeader()
    {
        if (_followsLeader) return;

        _followsLeader = true;
        partyService.LeaderChanged += (_, _) => playerSpawner.Refresh(gameStateService.Player);
    }

    // an entity a page sent to this map, standing on the tile it was sent to (unscaled, the scene scales it)
    private async Task<MapEntity> Arrive(VisitingEntity visitor)
    {
        var entity = jsonService.Copy(visitor.Entity)!;
        foreach (var page in entity.Pages)
        {
            if (page.Modules.OfType<PositionModule>().FirstOrDefault() is not { } position) continue;

            var image = page.Modules.OfType<CharacterImageModule>().FirstOrDefault();
            var (x, y) = await playerSpawner.StandOnTile(image?.ImageName, visitor.TileX, visitor.TileY);
            position.X = x;
            position.Y = y;
            // the saved direction's row says which character on the sheet it is, only the way it faces changes
            if (visitor.Direction is not Direction.None) position.Direction = (Direction) ((int) position.Direction / 4 * 4 + (int) visitor.Direction);
        }

        return new MapEntity(entity);
    }

    // the map being left: each entity whose page remembers its position keeps where it stands, the others forget
    private void RememberPlaces()
    {
        if (gameStateService.ActiveMap is not { } left) return;

        foreach (var entity in gameStateService.MapEntities.Where(x => x != gameStateService.Player))
        {
            var key = (left.Guid, entity.Entity.Guid);
            if (entity is { IsErased: false, ActivePage.RemembersPosition: true } && entity.GetModule<PositionModule>() is { } position)
            {
                gameStateService.EntityPlaces[key] = new EntityPlace(position.X, position.Y, position.Z, position.SortZ, position.Direction);
            }
            else gameStateService.EntityPlaces.Remove(key);
        }
    }

    // the page isn't picked until the first update, so every page that remembers gets the place, the active one uses it
    private void PutBack(GameMap map, MapEntity entity)
    {
        if (!gameStateService.EntityPlaces.TryGetValue((map.Guid, entity.Entity.Guid), out var place)) return;

        foreach (var position in entity.Entity.Pages.Where(x => x.RemembersPosition).SelectMany(x => x.Modules).OfType<PositionModule>())
        {
            position.X = place.X;
            position.Y = place.Y;
            position.Z = place.Z;
            position.SortZ = place.SortZ;
            position.Direction = place.Direction;
        }
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

    // the props placed on the map: each one sorts with the characters by its base (where it meets the ground), and its
    // footprint becomes collision like an area painted in the editor. A flat prop is drawn under everyone
    private async Task<(List<SceneSprite> sprites, List<MapArea> footprints)> LoadProps(GameMap map)
    {
        var scale = gameDataService.ScaleFactor;
        var folder = Path.Combine(locationService.GraphicsDirectory!, "Props");
        var sprites = new List<SceneSprite>();
        var footprints = new List<MapArea>();
        foreach (var placed in map.Props)
        {
            if (gameDataService.Props.FirstOrDefault(x => x.Guid == placed.PropGuid) is not { ImageName: { } imageName } prop) continue;

            var path = Path.Combine(folder, imageName);
            if (!File.Exists(path)) continue;

            // the base point is the bottom middle of the picture, everything below is unscaled map pixels
            var (width, height) = await graphicsService.GetImageDimensions(path);
            var left = placed.X - width / 2.0;
            var top = placed.Y - height;
            var level = placed.Level ?? SurfaceLevel(map, placed.X, placed.Y);
            sprites.Add(new SceneSprite(new PictureBox
            {
                // a flipped prop draws a mirrored copy of the picture, same size, so it stays where it was placed
                ImagePath = await graphicsService.GetPropImagePath(path, placed.Flip, prop.BuryDepth, prop.BuryFeather),
                Z = 0,
                ScaleX = (float) scale,
                ScaleY = (float) scale
            })
            {
                X = left * scale,
                Y = top * scale,
                Width = width,
                Height = height,
                SortY = prop.Flat ? null : (placed.Y + prop.SortOffset) * scale
            });

            foreach (var area in prop.Footprint)
            {
                var offsetX = placed.Flip ? width - area.OffsetX - area.Width : area.OffsetX;
                footprints.Add(new MapArea
                {
                    AreaType = MapAreaType.Collision,
                    OffsetX = (int) Math.Round(left + offsetX),
                    OffsetY = (int) Math.Round(top + area.OffsetY),
                    Width = area.Width,
                    Height = area.Height,
                    Level = level
                });
            }
        }

        return (sprites, footprints);
    }

    // a prop is on the level of the highest surface its base point is on (a crate on a raised platform), the ground otherwise.
    // The base point is the bottom of the picture, the pixel above it is the one standing on the map
    private static int SurfaceLevel(GameMap map, double x, double y)
    {
        return map.Areas
            .Where(area => area.IsSurface && x >= area.OffsetX && x < area.OffsetX + area.Width && y - 1 >= area.OffsetY && y - 1 < area.OffsetY + area.Height)
            .Select(area => area.Level)
            .DefaultIfEmpty(0)
            .Max();
    }

    // heat haze areas shimmer just over the ground layer: the ground bends, the props and characters on it don't
    private IEnumerable<SceneSprite> LoadHeatHazes(GameMap map)
    {
        var scale = gameDataService.ScaleFactor;
        var groundZ = (int) (map.LayerOptions.FirstOrDefault(x => x.Type == MapLayerType.Ground)?.Z ?? MapLayer.Defaults(MapLayerType.Ground).Z);
        foreach (var area in map.Areas.Where(x => x.AreaType == MapAreaType.HeatHaze && x.Width > 0 && x.Height > 0))
        {
            var haze = new HeatHaze
            {
                Z = groundZ,
                Width = (float) (area.Width * scale),
                Height = (float) (area.Height * scale),
                Strength = (float) (area.HazeStrength * scale),
                Scale = (float) (area.HazeRippleSize * scale),
                Speed = (float) (area.HazeSpeed * scale),
                Feather = (float) (Math.Min(16.0, Math.Min(area.Width, area.Height) / 4.0) * scale)
            };

            // no picture to size, it goes exactly where the area is. Sorted after the ground on the ground's z
            yield return new SceneSprite(haze)
            {
                X = area.OffsetX * scale,
                Y = area.OffsetY * scale,
                SortY = double.MaxValue
            };
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
            if (gameStateService.ActiveMap is { } map) UpdateMap(map, gameStateService.IsTransferring || screenLayers.IsPaused);
        }

        // loaded outside the lock, the load takes the lock itself once the map is ready
        if (transfer is not null) _ = Transfer(transfer);
        if (_loadBattle)
        {
            _loadBattle = false;
            _ = EnterBattle();
        }
    }

    private void UpdateMap(GameMap map, bool paused)
    {
        navigationService.NewUpdate();
        foreach (var system in _systems.Where(x => !paused || x.RunsWhilePaused))
        {
            foreach (var entity in gameStateService.MapEntities) system.Update(entity);
        }

        gameStateService.Player ??= gameStateService.MapEntities.FirstOrDefault(x => x.HasModule<InputModule>());

        // pages start after the systems picked the active pages and movement found what was touched, then the fight
        if (!paused)
        {
            pageRunner.Update();
            battleRunner.Update();
        }
        UpdateCamera(map);
        mapWeather.Update(Camera);
        screenLayers.Update(Camera);
        if (!paused)
        {
            foreach (var environment in _environments) environment.Update();
        }

        renderer.Render(_mapImages.Concat(_environments.SelectMany(x => x.Current)).Concat(CharacterSprites()).Concat(mapWeather.Sprites()).Concat(animationSystem.Sprites(gameStateService.MapEntities)).Concat(BalloonSprites()), Camera);
        debugOverlay.Update(map, Camera);
        if (paused) return;

        if (inputService.Lag > 0) inputService.Lag--;

        // Initiate Battle asked for a fight: the map pauses and fades out, the battle loads on black
        if (gameStateService.BattleRequest is { } request)
        {
            StartBattle(request);
            return;
        }


        // the map pauses (enemies, arrows, the player) for the whole transfer: fade out, load, fade in
        if (gameStateService.EdgeTransfer is not { } requested) return;
        gameStateService.EdgeTransfer = null;
        gameStateService.IsTransferring = true;
        _transfer = requested;
        _phase = TransferPhase.FadingOut;
        _fadeTimer = 0;
        mapAudio.Leave(requested.Map, FadeSeconds);
    }

    // moves the fades along (the map change's and a page's screen fade, the darker one shows), gives back the
    // transfer to load once the screen is fully black
    private EdgeTransfer? UpdateFade()
    {
        screenFade.Update();
        EdgeTransfer? transfer = null;
        switch (_phase)
        {
            case TransferPhase.FadingOut:
                if (++_fadeTimer < FadeUpdates) break;

                transfer = _transfer;
                _transfer = null;
                _loadBattle = _battleWaitingForFade;
                _battleWaitingForFade = false;
                _phase = TransferPhase.Loading;
                break;

            // the new map is drawn paused while it fades in, then everything carries on
            case TransferPhase.FadingIn:
                if (--_fadeTimer > 0) break;

                _phase = TransferPhase.None;
                gameStateService.IsTransferring = false;
                break;
        }

        var transferFade = _phase == TransferPhase.None ? 0.0 : (double) _fadeTimer / FadeUpdates;
        renderer.SetFade(Math.Max(transferFade, screenFade.Level));
        return transfer;
    }

    // a map that fails to load fades back in on the map the player was on instead of freezing the game
    private async Task Transfer(EdgeTransfer transfer)
    {
        try
        {
            if (transfer.OnTile) await Load(transfer.Map, new SpawnPoint { TileX = (int) transfer.X, TileY = (int) transfer.Y, Direction = transfer.Direction });
            else await Load(transfer.Map, new SpawnPoint { Direction = transfer.Direction }, new Point(transfer.X, transfer.Y));
        }
        catch
        {
            lock (_lock) _phase = TransferPhase.FadingIn;

            // the map's audio already started fading out, it comes back with the map
            if (gameStateService.ActiveMap is { } map) _ = mapAudio.Play(map, FadeSeconds);
        }
    }

    // remembers exactly where the player stands and how fast they walk, so the battle can put both back afterwards
    private void StartBattle(BattleRequest request)
    {
        gameStateService.BattleRequest = null;
        if (gameStateService.ActiveMap is not { } here) return;

        var scale = gameDataService.ScaleFactor;
        var position = gameStateService.Player?.GetModule<PositionModule>();
        var speed = gameStateService.Player?.GetModule<MovementModule>()?.Speed ?? new MovementModule().Speed;
        var facing = position is null ? Direction.Down : (Direction) ((int) position.Direction % 4);
        gameStateService.Battle = new ActiveBattle(request, here, (position?.X ?? 0) / scale, (position?.Y ?? 0) / scale, facing, speed);

        gameStateService.IsTransferring = true;
        _battleWaitingForFade = true;
        _phase = TransferPhase.FadingOut;
        _fadeTimer = 0;
        mapAudio.Leave(request.BattleMap, FadeSeconds);
    }

    // the battle map with the party on its spawn points and the troop's enemies on theirs. A battle that fails to load
    // fades back in on the story map instead of freezing the game
    private async Task EnterBattle()
    {
        if (gameStateService.Battle is not { } battle) return;

        try
        {
            var lineup = await battleSpawner.Lineup(battle.Request.Troop);
            await Load(battle.Request.BattleMap, new SpawnPoint { Direction = lineup.LeaderFacing }, lineup.LeaderAt, lineup.Bodies, Math.Max(1, battle.StorySpeed - 1));

            // the roster's bodies are on the map now, the leader's is the player
            lock (_lock)
            {
                foreach (var entry in lineup.Roster)
                {
                    var body = entry.Body is null ? gameStateService.Player : gameStateService.MapEntities.FirstOrDefault(x => x.Entity == entry.Body);
                    if (body is null) continue;

                    var team = entry.IsEnemy ? BattleFighter.EnemyTeam : BattleFighter.PartyTeam;
                    battle.Fighters.Add(new BattleFighter(body, entry.Stats, entry.IsEnemy, entry.Body is null, team));
                }
            }
        }
        catch
        {
            lock (_lock)
            {
                gameStateService.Battle = null;
                _phase = TransferPhase.FadingIn;
            }

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

    private IEnumerable<SceneSprite> BalloonSprites()
    {
        return gameStateService.MapEntities
            .Select(x => x.State<BalloonState>().Sprite)
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
