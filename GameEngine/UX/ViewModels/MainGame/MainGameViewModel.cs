using System.Collections.ObjectModel;
using VirtualKey = Windows.System.VirtualKey;
using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Update;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using MercuryLibrary.ImageComponents;
using Microsoft.UI.Dispatching;

namespace GameEngine.UX.ViewModels.MainGame;

/// <summary>
/// The game being played on the map. Every update moves the entities, follows the player with the camera
/// and hands the canvas its pictures in z order.
/// </summary>
public partial class MainGameViewModel(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IUpdateService updateService,
    IInputService inputService,
    IGraphicsService graphicsService,
    ILocationService locationService) : BaseViewModel
{
    #region Fields

    // the canvas updates on its own thread, a map transfer on the ui thread can't happen mid update
    private readonly Lock _lock = new();
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly List<MapImage> _mapImages = [];
    private readonly MapEntity _camera = new(new Camera());
    private MapEntity? _cameraFollow;
    private PictureBox[] _shownNodes = [];

    #endregion

    #region Properties

    // what the canvas draws, in draw order
    public ObservableCollection<PictureBox> Nodes { get; } = [];

    // how far the map is shifted to keep the player in view
    public double CameraX { get; private set; }

    public double CameraY { get; private set; }

    public double GameWidth => gameDataService.GameWidth;

    public double GameHeight => gameDataService.GameHeight;

    public double FrameRate => gameDataService.FrameRate;

    #endregion

    #region Commands

    public RelayCommand<VirtualKey> KeyDownCommand => field ??= new RelayCommand<VirtualKey>(inputService.KeyPressed);

    public RelayCommand<VirtualKey> KeyUpCommand => field ??= new RelayCommand<VirtualKey>(inputService.KeyReleased);

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        OnPropertyChanged(nameof(GameWidth));
        OnPropertyChanged(nameof(GameHeight));
        await TransferMap(1);
    }

    #endregion

    #region Update

    protected override void Update()
    {
        lock (_lock)
        {
            if (gameStateService.IsTransferring || gameStateService.ActiveMap is null) return;

            gameStateService.Player ??= gameStateService.MapEntities.FirstOrDefault(x => x.HasModule<InputModule>());
            UpdateEntities();
            UpdateCamera();
            UpdateImages();

            if (inputService.Lag > 0) inputService.Lag--;
        }
    }

    private void UpdateEntities()
    {
        foreach (var entity in gameStateService.MapEntities)
        {
            updateService.UpdateActivePage(entity);
            updateService.UpdateEntity(entity);
        }
    }

    // the map images and character pictures, lowest z first, shifted by the camera
    private void UpdateImages()
    {
        foreach (var image in _mapImages)
        {
            image.Picture.X = (int) Math.Round(image.X + CameraX);
            image.Picture.Y = (int) Math.Round(image.Y + CameraY);
        }

        var characterImages = gameStateService.MapEntities
            .Where(x => x.HasModule<CharacterImageModule>() && x.Picture is not null)
            .Select(x =>
            {
                x.Picture!.X = (int) Math.Round(x.PictureX + CameraX);
                x.Picture.Y = (int) Math.Round(x.PictureY + CameraY);
                return x.Picture;
            });

        // TODO - draw the shown hitboxes (ModuleService.GetMapShownHitboxes), the canvas only draws pictures
        var ordered = _mapImages.Select(x => x.Picture).Concat(characterImages).OrderBy(x => x.Z).ToArray();
        SetNodes(ordered);
    }

    // the canvas only needs the list again when a picture is added, removed or changes places
    private void SetNodes(PictureBox[] ordered)
    {
        if (ordered.SequenceEqual(_shownNodes)) return;
        _shownNodes = ordered;

        // the collection belongs to the canvas on the ui thread, never Remove: the canvas disposes the bitmap on Remove
        _dispatcherQueue.TryEnqueue(() =>
        {
            Nodes.Clear();
            foreach (var node in ordered) Nodes.Add(node);
        });
    }

    #endregion

    #region Methods

    public async Task TransferMap(int mapId)
    {
        var map = gameDataService.GameMaps.FirstOrDefault(x => x.Id == mapId && !x.FolderOnly);
        if (map is null) return;

        lock (_lock)
        {
            gameStateService.IsTransferring = true;
        }

        // the images are measured off the game loop, then swapped in while it is held
        var mapImages = await LoadMapImages(map);
        var entities = map.Entities.Select(x => new MapEntity(x)).ToList();
        await updateService.LoadCharacterSheets(entities);

        lock (_lock)
        {
            gameStateService.ActiveMap = map;
            gameStateService.Player = null;
            _cameraFollow = null;
            InstallMapImages(mapImages);
            InstallMapAreas(map.Areas);
            InstallMapEntities(entities);
            gameStateService.IsTransferring = false;
        }
    }

    // a layer is only drawn when its image exists for this map id, the map's layer options change its defaults
    private async Task<List<MapImage>> LoadMapImages(GameMap map)
    {
        var scale = gameDataService.ScaleFactor;
        var images = new List<MapImage>();
        foreach (var type in Enum.GetValues<MapLayerType>())
        {
            var path = new MapLayer(type).ImagePath(locationService.GraphicsDirectory!, map.Id);
            if (!File.Exists(path)) continue;

            var defaults = MapLayer.Defaults(type);
            var option = map.LayerOptions.FirstOrDefault(x => x.Type == type);
            var picture = new PictureBox
            {
                ImagePath = path,
                Z = (int) (option?.Z ?? defaults.Z),
                Opacity = (float) (option?.Opacity ?? defaults.Opacity),
                BlendMode = option is null ? defaults.MapBlendMode : option.MapBlendMode,
                ScaleX = (float) scale,
                ScaleY = (float) scale
            };

            // the canvas scales a picture around its center, this keeps the scaled map's top left at 0, 0
            var (width, height) = await graphicsService.GetImageDimensions(path);
            images.Add(new MapImage(picture, width * (scale - 1.0) / 2.0, height * (scale - 1.0) / 2.0));
        }

        return [.. images.OrderBy(x => x.Picture.Z)];
    }

    private void InstallMapImages(List<MapImage> images)
    {
        _mapImages.Clear();
        _mapImages.AddRange(images);
    }

    private void InstallMapAreas(IEnumerable<MapArea> areas)
    {
        gameStateService.MapAreas.Clear();
        gameStateService.MapAreas.AddRange(areas);
    }

    private void InstallMapEntities(List<MapEntity> entities)
    {
        gameStateService.MapEntities.Clear();
        gameStateService.MapEntities.AddRange(entities);
        gameStateService.MapEntities.Add(_camera);
    }

    #endregion

    #region Camera

    private void UpdateCamera()
    {
        if (_camera.GetModule<PositionModule>() is not { } positionModule) return;
        _cameraFollow ??= gameStateService.Player;
        if (_cameraFollow is null) return;

        var followPosition = _cameraFollow.GetModule<PositionModule>();
        var followImage = _cameraFollow.GetModule<CharacterImageModule>();
        var followHitboxes = _cameraFollow.GetModule<EntityHitboxModule>();
        if (followPosition is null || followImage is null || followHitboxes is null)
        {
            positionModule.X = 0;
            positionModule.Y = 0;
        }
        else
        {
            positionModule.X = followPosition.X;
            positionModule.Y = followPosition.Y;
        }

        var followHitbox = FollowHitbox(followHitboxes);
        var cameraWidthDistance = CalculateCameraWidthDistance(positionModule, followHitbox);
        var cameraHeightDistance = CalculateCameraHeightDistance(positionModule, followHitbox);
        CameraX = cameraWidthDistance + CalculateXEdgeStop(cameraWidthDistance);
        CameraY = cameraHeightDistance + CalculateYEdgeStop(cameraHeightDistance);
    }

    // the camera centers on the followed entity's first hitbox
    private MapHitbox? FollowHitbox(EntityHitboxModule? hitboxModule)
    {
        if (_cameraFollow is null || hitboxModule?.Hitboxes.GetValueOrDefault(Direction.None)?.FirstOrDefault() is not { } area) return null;
        return _cameraFollow.GetHitbox(area);
    }

    private double CalculateCameraWidthDistance(PositionModule positionModule, MapHitbox? hitbox)
    {
        var cx = -positionModule.X;
        var cw = hitbox?.Width ?? 0.0;
        return cx - (cw / 2.0) + (gameDataService.GameWidth / 2.0);
    }

    private double CalculateCameraHeightDistance(PositionModule positionModule, MapHitbox? hitbox)
    {
        var cy = -positionModule.Y;
        var ch = hitbox?.Height ?? 0.0;
        return cy - (ch / 2.0) + (gameDataService.GameHeight / 2.0);
    }

    // stops the camera at the left and right of the map
    private double CalculateXEdgeStop(double cameraWidthDistance)
    {
        if (gameStateService.ActiveMap is not { } map) return 0.0;

        var hidden = (map.Width * gameDataService.GridSize * gameDataService.ScaleFactor) - gameDataService.GameWidth;
        if (cameraWidthDistance >= 0.0) return -cameraWidthDistance;
        if (cameraWidthDistance <= -hidden) return -cameraWidthDistance - hidden;
        return 0.0;
    }

    // stops the camera at the top and bottom of the map
    private double CalculateYEdgeStop(double cameraHeightDistance)
    {
        if (gameStateService.ActiveMap is not { } map) return 0.0;

        var hidden = (map.Height * gameDataService.GridSize * gameDataService.ScaleFactor) - gameDataService.GameHeight;
        if (cameraHeightDistance >= 0.0) return -cameraHeightDistance;
        if (cameraHeightDistance <= -hidden) return -cameraHeightDistance - hidden;
        return 0.0;
    }

    #endregion

    #region Types

    // a full map image and where its top left sits before the camera moves it
    private sealed record MapImage(PictureBox Picture, double X, double Y);

    #endregion
}
