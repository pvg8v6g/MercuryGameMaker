using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation;
using Windows.System;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Media;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Services.Media;
using GameLibrary.Utilities.Calculations;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using MercuryLibrary.Enumerations;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.Extensions;
using Page = GameLibrary.Models.Page;

namespace GameMaker.UX.ViewModels.MainPage;

public partial class MainPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    IMediaService mediaService,
    IAudioService audioService,
    INavigationService navigationService) : BaseViewModel
{
    #region Constants

    private const string None = "(None)";

    #endregion

    #region Fields

    private readonly Dictionary<Guid, MapTreeNode> _nodes = [];
    private bool _rebuildingTree;

    // canvas interaction state
    private Point? _panStart;
    private Point _panCamera;
    private MapEntityModel? _dragEntity;
    private MapAreaModel? _dragArea;
    private Point _dragAreaOffset;
    private Point? _drawStart;
    private Point _contextTile;

    private Entity? _copiedEntity;

    #endregion

    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public ObservableCollection<MapTreeNode> MapTree { get; } = [];

    public MapTreeNode? SelectedNode
    {
        get;
        set
        {
            // the tree view pushes null while it is rebuilt, keep the current map
            if (value is null && _rebuildingTree) return;
            var mapChanged = field?.Map.Guid != value?.Map.Guid;
            SetField(ref field, value);
            if (!mapChanged) return;

            OnPropertyChanged(nameof(SelectedMap));
            _ = OnSelectedMapChanged();
        }
    }

    public GameMap? SelectedMap => SelectedNode?.Map;

    public bool IsMapSelected => SelectedMap is { FolderOnly: false };

    public bool IsFolderSelected => SelectedMap is { FolderOnly: true };

    public GameMap? CopiedMap
    {
        get;
        private set => SetField(ref field, value);
    }

    public int TileSize => gameDataService.GridSize;

    public int MapPixelWidth => (SelectedMap?.Width ?? 0) * TileSize;

    public int MapPixelHeight => (SelectedMap?.Height ?? 0) * TileSize;

    #endregion

    #region Map Settings Properties

    public int MapWidth
    {
        get => SelectedMap?.Width ?? 0;
        set
        {
            if (SelectedMap is null || value <= 0 || SelectedMap.Width == value) return;
            SelectedMap.Width = value;
            OnMapSizeChanged();
        }
    }

    public int MapHeight
    {
        get => SelectedMap?.Height ?? 0;
        set
        {
            if (SelectedMap is null || value <= 0 || SelectedMap.Height == value) return;
            SelectedMap.Height = value;
            OnMapSizeChanged();
        }
    }

    public MapOption[] ParentOptions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public MapOption? SelectedParent
    {
        get => ParentOptions.FirstOrDefault(x => x.Guid == SelectedMap?.ParentGuid);
        set
        {
            if (value is null || SelectedMap is null || SelectedMap.ParentGuid == value.Guid) return;
            SelectedMap.ParentGuid = value.Guid;
            if (value.Guid is { } parent && _nodes.TryGetValue(parent, out var parentNode)) parentNode.IsExpanded = true;
            RebuildTree(SelectedMap.Guid);
        }
    }

    public MapOption[] ConnectionOptions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public MapOption? NorthMap
    {
        get => FindConnection(SelectedMap?.North);
        set => SetConnection(value, (map, guid) => map.North = guid);
    }

    public MapOption? EastMap
    {
        get => FindConnection(SelectedMap?.East);
        set => SetConnection(value, (map, guid) => map.East = guid);
    }

    public MapOption? SouthMap
    {
        get => FindConnection(SelectedMap?.South);
        set => SetConnection(value, (map, guid) => map.South = guid);
    }

    public MapOption? WestMap
    {
        get => FindConnection(SelectedMap?.West);
        set => SetConnection(value, (map, guid) => map.West = guid);
    }

    #endregion

    #region Layer Properties

    // ground, parallax and light for the selected map, in draw order
    public MapLayerModel[] Layers
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    #endregion

    #region Audio Properties

    public string[] BgmFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string[] BgsFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string[] FootstepFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string? SelectedBgm
    {
        get => SelectedMap?.Bgm?.Name ?? None;
        set
        {
            if (value is null || SelectedMap is null) return;
            SelectedMap.Bgm = ToAudioFile(SelectedMap.Bgm, value, "BGM");
            PlayBgmCommand.RaiseCanExecuteChanged();
        }
    }

    public string? SelectedBgs
    {
        get => SelectedMap?.Bgs?.Name ?? None;
        set
        {
            if (value is null || SelectedMap is null) return;
            SelectedMap.Bgs = ToAudioFile(SelectedMap.Bgs, value, "BGS");
            PlayBgsCommand.RaiseCanExecuteChanged();
        }
    }

    public string? SelectedFootstep
    {
        get => SelectedArea?.Area.Footstep?.Name ?? None;
        set
        {
            if (value is null || SelectedArea is null) return;
            SelectedArea.Area.Footstep = ToAudioFile(SelectedArea.Area.Footstep, value, "Footsteps");
        }
    }

    #endregion

    #region Canvas Properties

    public MapScene Scene { get; } = new();

    public ObservableCollection<PictureBox> Sprites => Scene.Sprites;

    public EditorCamera Camera { get; } = new();

    // the overlay is drawn in world space and shifted opposite the camera
    public double OverlayX => -Camera.X;

    public double OverlayY => -Camera.Y;

    public Rect ViewportRect => new(0, 0, Camera.ViewWidth, Camera.ViewHeight);

    public ObservableCollection<GridLine> GridLines { get; } = [];

    public ObservableCollection<MapEntityModel> Entities { get; } = [];

    public ObservableCollection<MapAreaModel> Areas { get; } = [];

    public MapEditMode EditMode
    {
        get;
        set
        {
            if (field == value) return;
            SetField(ref field, value);
            OnPropertyChanged(nameof(IsEntityMode));
            OnPropertyChanged(nameof(IsAreaMode));
            Select(null);
        }
    } = MapEditMode.Entities;

    public bool IsEntityMode
    {
        get => EditMode == MapEditMode.Entities;
        set
        {
            if (value) EditMode = MapEditMode.Entities;
        }
    }

    public bool IsAreaMode
    {
        get => EditMode == MapEditMode.Areas;
        set
        {
            if (value) EditMode = MapEditMode.Areas;
        }
    }

    public MapAreaType[] AreaTypes { get; } = Enum.GetValues<MapAreaType>();

    // the type given to newly drawn areas
    public MapAreaType NewAreaType
    {
        get;
        set => SetField(ref field, value);
    } = MapAreaType.Collision;

    public bool ShowGrid
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool ShowAreas
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool ShowParallax
    {
        get;
        set
        {
            SetField(ref field, value);
            RebuildScene();
        }
    } = true;

    public bool ShowLight
    {
        get;
        set
        {
            SetField(ref field, value);
            RebuildScene();
        }
    } = true;

    public double HoverX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double HoverY
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool IsHovering
    {
        get;
        private set => SetField(ref field, value);
    }

    public Rect DrawPreview
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool IsDrawing
    {
        get;
        private set => SetField(ref field, value);
    }

    public string CursorText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    #endregion

    #region Selection Properties

    public MapEntityModel? SelectedEntity
    {
        get;
        private set
        {
            field?.IsSelected = false;
            SetField(ref field, value);
            field?.IsSelected = true;
            OnPropertyChanged(nameof(EntityName));
            OnPropertyChanged(nameof(EntityZ));
            OnSelectionChanged();
        }
    }

    public MapAreaModel? SelectedArea
    {
        get;
        private set
        {
            field?.IsSelected = false;
            SetField(ref field, value);
            field?.IsSelected = true;
            OnPropertyChanged(nameof(SelectedFootstep));
            OnSelectionChanged();
        }
    }

    public bool HasNoSelection => SelectedEntity is null && SelectedArea is null;

    public string EntityName
    {
        get => SelectedEntity?.Name ?? string.Empty;
        set
        {
            if (SelectedEntity is null || SelectedEntity.Entity.Name == value) return;
            SelectedEntity.Entity.Name = value;
            SelectedEntity.Refresh();
            OnPropertyChanged();
        }
    }

    public double EntityZ
    {
        get => SelectedEntity?.Z ?? 0;
        set
        {
            if (SelectedEntity is null || double.IsNaN(value) || SelectedEntity.Z == value) return;
            SelectedEntity.Z = value;
            OnPropertyChanged();
            RebuildScene();
        }
    }

    #endregion

    #region Commands

    public RelayCommand NewMapCommand => field ??= new RelayCommand(() => NewMap(false));

    public RelayCommand NewFolderCommand => field ??= new RelayCommand(() => NewMap(true));

    public RelayCommand CopyMapCommand => field ??= new RelayCommand(CopyMap, () => SelectedMap is not null);

    public RelayCommand PasteMapCommand => field ??= new RelayCommand(PasteMap);

    public RelayCommand DeleteMapCommand => field ??= new RelayCommand(DeleteMap, () => SelectedMap is not null);

    public AsyncRelayCommand FitToGroundCommand => field ??= new AsyncRelayCommand(FitToGround);

    public RelayCommand<Size> CanvasSizeChangedCommand => field ??= new RelayCommand<Size>(OnCanvasSizeChanged);

    public RelayCommand<PointerEventInfo> CanvasPressedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasPressed);

    public RelayCommand<PointerEventInfo> CanvasMovedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasMoved);

    public RelayCommand<PointerEventInfo> CanvasReleasedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasReleased);

    public RelayCommand<PointerEventInfo> CanvasWheelCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasWheel);

    public AsyncRelayCommand<Point> CanvasDoubleTappedCommand => field ??= new AsyncRelayCommand<Point>(OnCanvasDoubleTapped);

    public AsyncRelayCommand NewEntityCommand => field ??= new AsyncRelayCommand(() => NewEntity(_contextTile), () => IsMapSelected && IsEntityMode);

    public AsyncRelayCommand EditEntityCommand => field ??= new AsyncRelayCommand(EditEntity, () => SelectedEntity is not null);

    public RelayCommand CopySelectionCommand => field ??= new RelayCommand(CopySelection, () => SelectedEntity is not null);

    public RelayCommand PasteSelectionCommand => field ??= new RelayCommand(PasteSelection, () => _copiedEntity is not null && IsMapSelected && IsEntityMode);

    public RelayCommand DeleteSelectionCommand => field ??= new RelayCommand(DeleteSelection, () => !HasNoSelection);

    public RelayCommand ClearSelectionCommand => field ??= new RelayCommand(() => Select(null));

    public RelayCommand PlayBgmCommand => field ??= new RelayCommand(PlayBgm, () => SelectedMap?.Bgm is not null);

    public RelayCommand PlayBgsCommand => field ??= new RelayCommand(PlayBgs, () => SelectedMap?.Bgs is not null);

    public RelayCommand StopAudioCommand => field ??= new RelayCommand(StopAudio);

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        // rebuilt every visit so newly added graphics and media show up
        BgmFiles = [None, .. mediaService.GetMediaFiles(Cache.Media)];
        BgsFiles = [None, .. GetFiles(mediaService.GetBgsPath())];
        FootstepFiles = [None, .. mediaService.GetMediaFiles(Cache.Footsteps)];

        var selected = SelectedMap?.Guid ?? gameDataService.GameMaps.OrderBy(x => x.Id).FirstOrDefault(x => !x.FolderOnly)?.Guid;
        RebuildTree(selected);
        RefreshMapSettings();

        // entities may have been edited elsewhere (e.g. the character image of a page)
        await RefreshEntitySprites();
    }

    private async Task OnSelectedMapChanged()
    {
        Select(null);
        RefreshMapSettings();
        RebuildGrid();
        RebuildModels();

        Camera.X = 0;
        Camera.Y = 0;
        OnCameraChanged();

        await RefreshEntitySprites();
    }

    #endregion

    #region Map Tree

    private void NewMap(bool folder)
    {
        var parent = SelectedMap;
        var map = new GameMap
        {
            Id = Calculations.GetNextId([.. gameDataService.GameMaps.Select(x => x.Id)]),
            Name = folder ? "New Folder" : "New Map",
            FolderOnly = folder,
            ParentGuid = parent?.Guid
        };

        parent?.IsExpanded = true;
        gameDataService.GameMaps.Add(map);
        RebuildTree(map.Guid);
    }

    private void CopyMap()
    {
        CopiedMap = SelectedMap;
    }

    private void PasteMap()
    {
        if (CopiedMap is null || jsonService.Clone(CopiedMap) is not { } clone) return;

        clone.Guid = Guid.NewGuid();
        clone.Id = Calculations.GetNextId([.. gameDataService.GameMaps.Select(x => x.Id)]);
        clone.IsExpanded = false;
        foreach (var entity in clone.Entities) ReassignGuids(entity, clone.Guid);
        foreach (var area in clone.Areas) area.Guid = Guid.NewGuid();

        gameDataService.GameMaps.Add(clone);
        RebuildTree(clone.Guid);
    }

    // children move up to the deleted map's parent so deleting a folder never deletes the maps inside it
    private void DeleteMap()
    {
        if (SelectedMap is not { } map) return;

        foreach (var other in gameDataService.GameMaps)
        {
            if (other.ParentGuid == map.Guid) other.ParentGuid = map.ParentGuid;
            if (other.North == map.Guid) other.North = null;
            if (other.East == map.Guid) other.East = null;
            if (other.South == map.Guid) other.South = null;
            if (other.West == map.Guid) other.West = null;
        }

        gameDataService.GameMaps.Remove(map);
        if (CopiedMap == map) CopiedMap = null;

        RebuildTree(map.ParentGuid ?? gameDataService.GameMaps.OrderBy(x => x.Id).FirstOrDefault()?.Guid);
    }

    private void RebuildTree(Guid? select)
    {
        _rebuildingTree = true;

        _nodes.Clear();
        MapTree.Clear();
        foreach (var map in gameDataService.GameMaps.OrderBy(x => x.Id)) _nodes[map.Guid] = new MapTreeNode(map);

        foreach (var node in _nodes.Values)
        {
            if (HasValidParent(node.Map) && _nodes.TryGetValue(node.Map.ParentGuid!.Value, out var parent)) parent.Children.Add(node);
            else MapTree.Add(node);
        }

        _rebuildingTree = false;

        MapTreeNode? selected = null;
        if (select is { } guid) _nodes.TryGetValue(guid, out selected);
        selected ??= MapTree.FirstOrDefault();

        // the tree can only select a node whose parents are open
        for (var parent = selected?.Map.ParentGuid; parent is { } p && _nodes.TryGetValue(p, out var parentNode); parent = parentNode.Map.ParentGuid)
        {
            parentNode.IsExpanded = true;
        }

        // the node objects are new, the same map keeps its state and the tree selects the new instance
        SelectedNode = selected;
        RefreshMapOptions();
        CopyMapCommand.RaiseCanExecuteChanged();
        DeleteMapCommand.RaiseCanExecuteChanged();
    }

    // a parent that no longer exists or would create a loop puts the map at the root
    private bool HasValidParent(GameMap map)
    {
        var visited = new HashSet<Guid> { map.Guid };
        for (var parent = map.ParentGuid; parent is { } p; parent = _nodes[p].Map.ParentGuid)
        {
            if (!_nodes.ContainsKey(p) || !visited.Add(p)) return false;
        }

        return map.ParentGuid is not null;
    }

    #endregion

    #region Map Settings

    private void RefreshMapOptions()
    {
        var maps = gameDataService.GameMaps.OrderBy(x => x.Id).ToArray();
        var excluded = SelectedMap is null ? [] : Descendants(SelectedMap.Guid).Append(SelectedMap.Guid).ToHashSet();

        ParentOptions = [new MapOption(null, "(Root)"), .. maps.Where(x => !excluded.Contains(x.Guid)).Select(ToOption)];
        ConnectionOptions = [new MapOption(null, None), .. maps.Where(x => !x.FolderOnly && x.Guid != SelectedMap?.Guid).Select(ToOption)];

        OnPropertyChanged(nameof(SelectedParent));
        OnPropertyChanged(nameof(NorthMap));
        OnPropertyChanged(nameof(EastMap));
        OnPropertyChanged(nameof(SouthMap));
        OnPropertyChanged(nameof(WestMap));
    }

    private void RefreshMapSettings()
    {
        RefreshMapOptions();
        OnPropertyChanged(nameof(IsMapSelected));
        OnPropertyChanged(nameof(IsFolderSelected));
        OnPropertyChanged(nameof(MapWidth));
        OnPropertyChanged(nameof(MapHeight));
        OnPropertyChanged(nameof(MapPixelWidth));
        OnPropertyChanged(nameof(MapPixelHeight));
        RefreshLayers();
        OnPropertyChanged(nameof(SelectedBgm));
        OnPropertyChanged(nameof(SelectedBgs));
        PlayBgmCommand.RaiseCanExecuteChanged();
        PlayBgsCommand.RaiseCanExecuteChanged();
        NewEntityCommand.RaiseCanExecuteChanged();
        PasteSelectionCommand.RaiseCanExecuteChanged();
    }

    private void OnMapSizeChanged()
    {
        OnPropertyChanged(nameof(MapWidth));
        OnPropertyChanged(nameof(MapHeight));
        OnPropertyChanged(nameof(MapPixelWidth));
        OnPropertyChanged(nameof(MapPixelHeight));
        RebuildGrid();
        OnCameraChanged();
    }

    private async Task FitToGround()
    {
        if (SelectedMap is null || Layers.FirstOrDefault(x => x.Layer.Type == MapLayerType.Ground) is not { HasImage: true } ground) return;

        var (width, height) = await graphicsService.GetImageDimensions(ground.ImagePath);
        SelectedMap.Width = Math.Max(1, (int) Math.Ceiling(width / TileSize));
        SelectedMap.Height = Math.Max(1, (int) Math.Ceiling(height / TileSize));
        OnMapSizeChanged();
    }

    private MapOption? FindConnection(Guid? guid)
    {
        return ConnectionOptions.FirstOrDefault(x => x.Guid == guid);
    }

    private void SetConnection(MapOption? option, Action<GameMap, Guid?> apply)
    {
        // the combo box pushes null while its items are swapped
        if (option is null || SelectedMap is null) return;
        apply(SelectedMap, option.Guid);
    }

    private IEnumerable<Guid> Descendants(Guid guid)
    {
        foreach (var child in gameDataService.GameMaps.Where(x => x.ParentGuid == guid))
        {
            yield return child.Guid;
            foreach (var descendant in Descendants(child.Guid)) yield return descendant;
        }
    }

    private static MapOption ToOption(GameMap map) => new(map.Guid, $"{map.Id:000}: {map.Name}");

    #endregion

    #region Layers

    // the images can be added or removed outside the editor so they are looked up again every time
    private void RefreshLayers()
    {
        foreach (var layer in Layers) layer.PropertyChanged -= OnLayerChanged;

        Layers = SelectedMap is { FolderOnly: false } map
            ?
            [
                .. Enum.GetValues<MapLayerType>()
                    .Select(type => new MapLayerModel(map, type, locationService.GraphicsDirectory!))
                    .OrderBy(x => x.Z)
            ]
            : [];

        foreach (var layer in Layers) layer.PropertyChanged += OnLayerChanged;
    }

    private void OnLayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        // raised once for every change to a layer's z, opacity or blend
        if (e.PropertyName == nameof(MapLayerModel.IsDefault)) RebuildScene();
    }

    private bool IsLayerShown(MapLayer layer) => layer.Type switch
    {
        MapLayerType.Parallax => ShowParallax,
        MapLayerType.Light => ShowLight,
        _ => true
    };

    #endregion

    #region Scene

    // the scene sorts by z, so layers and entities interleave however their z values say (layer defaults come from the MapLayer attribute on MapLayerType)
    private void RebuildScene()
    {
        Scene.Clear();
        if (!IsMapSelected) return;

        foreach (var layer in Layers.Where(x => IsLayerShown(x.Layer))) Scene.AddLayer(layer.Layer, layer.ImagePath);

        foreach (var entity in Entities)
        {
            entity.Picture = null;
            if (entity.Sprite is not { ImagePath: { } path, Rect: { } rect }) continue;
            var (x, y) = SpritePosition(entity, rect);
            entity.Picture = Scene.Add(path, x, y, entity.DrawZ, rect);
        }

        Scene.ApplyCamera(Camera);
    }

    // sprites stand on the bottom center of their tile so tall characters overlap the tile above
    private (double x, double y) SpritePosition(MapEntityModel entity, Rect rect)
    {
        return (entity.X + ((TileSize - rect.Width) / 2.0), entity.Y + TileSize - rect.Height);
    }

    private void RebuildModels()
    {
        Entities.Clear();
        Areas.Clear();
        if (!IsMapSelected || SelectedMap is not { } map) return;

        foreach (var entity in map.Entities) Entities.Add(new MapEntityModel(entity, TileSize));
        foreach (var area in map.Areas) Areas.Add(new MapAreaModel(area, TileSize));
    }

    private async Task RefreshEntitySprites()
    {
        var map = SelectedMap;
        await Task.WhenAll(Entities.ToArray().Select(LoadSprite));

        // the user may have picked another map while the sprites loaded
        if (map == SelectedMap) RebuildScene();
    }

    private async Task LoadSprite(MapEntityModel entity)
    {
        if (entity.CharacterImage is not { ImageName: { } imageName } image)
        {
            entity.Sprite = null;
            return;
        }

        try
        {
            entity.Sprite = await graphicsService.GetCharacter(imageName, image.Index, entity.Direction);
        }
        catch
        {
            // a missing or broken character sheet falls back to the overlay marker
            entity.Sprite = null;
        }
    }

    private void RebuildGrid()
    {
        GridLines.Clear();
        if (!IsMapSelected) return;

        for (var x = 0; x <= MapPixelWidth; x += TileSize) GridLines.Add(new GridLine(x, 0, x, MapPixelHeight));
        for (var y = 0; y <= MapPixelHeight; y += TileSize) GridLines.Add(new GridLine(0, y, MapPixelWidth, y));
    }

    private void OnCameraChanged()
    {
        Camera.Clamp(MapPixelWidth, MapPixelHeight);
        Scene.ApplyCamera(Camera);
        OnPropertyChanged(nameof(OverlayX));
        OnPropertyChanged(nameof(OverlayY));
    }

    #endregion

    #region Canvas Input

    private void OnCanvasSizeChanged(Size size)
    {
        Camera.ViewWidth = size.Width;
        Camera.ViewHeight = size.Height;
        OnPropertyChanged(nameof(ViewportRect));
        OnCameraChanged();
    }

    private void OnCanvasPressed(PointerEventInfo info)
    {
        var properties = info.Properties;
        if (properties.IsMiddleButtonPressed)
        {
            _panStart = info.Position;
            _panCamera = new Point(Camera.X, Camera.Y);
            return;
        }

        if (!IsMapSelected) return;

        var world = ToWorld(info.Position);
        if (!IsInsideMap(world))
        {
            Select(null);
            return;
        }

        var tile = ToTile(world);
        _contextTile = tile;
        var canDrag = properties.IsLeftButtonPressed;

        if (IsEntityMode)
        {
            var entity = Entities.LastOrDefault(x => x.Contains(world.X, world.Y));
            Select(entity);
            if (canDrag) _dragEntity = entity;
            return;
        }

        var area = Areas.LastOrDefault(x => x.Contains(world.X, world.Y));
        Select(area);
        if (!canDrag) return;

        if (area is not null)
        {
            _dragArea = area;
            _dragAreaOffset = new Point(tile.X - area.X, tile.Y - area.Y);
            return;
        }

        _drawStart = tile;
        UpdateDrawPreview(tile);
    }

    private void OnCanvasMoved(PointerEventInfo info)
    {
        if (_panStart is { } start)
        {
            Camera.X = _panCamera.X - (info.Position.X - start.X);
            Camera.Y = _panCamera.Y - (info.Position.Y - start.Y);
            OnCameraChanged();
            return;
        }

        if (!IsMapSelected) return;

        var world = ToWorld(info.Position);
        IsHovering = IsInsideMap(world);
        var tile = ToTile(world);
        if (IsHovering)
        {
            HoverX = tile.X;
            HoverY = tile.Y;
            CursorText = $"Tile {(int) (tile.X / TileSize)}, {(int) (tile.Y / TileSize)}";
            _contextTile = tile;
        }

        if (_dragEntity is not null && (_dragEntity.X != tile.X || _dragEntity.Y != tile.Y))
        {
            _dragEntity.MoveTo(tile.X, tile.Y);
            if (_dragEntity.Picture is { } picture && _dragEntity.Sprite?.Rect is { } rect)
            {
                var (x, y) = SpritePosition(_dragEntity, rect);
                Scene.Move(picture, x, y, Camera);
            }
        }
        else if (_dragArea is not null)
        {
            var x = Math.Clamp(tile.X - _dragAreaOffset.X, 0, Math.Max(0, MapPixelWidth - _dragArea.Width));
            var y = Math.Clamp(tile.Y - _dragAreaOffset.Y, 0, Math.Max(0, MapPixelHeight - _dragArea.Height));
            _dragArea.MoveTo((int) x, (int) y);
        }
        else if (_drawStart is not null)
        {
            UpdateDrawPreview(tile);
        }
    }

    private void OnCanvasReleased(PointerEventInfo info)
    {
        _panStart = null;
        _dragArea = null;

        // re-sort so the moved entity draws in front of/behind its new neighbours
        if (_dragEntity is not null)
        {
            _dragEntity = null;
            RebuildScene();
        }

        if (_drawStart is null) return;
        _drawStart = null;
        IsDrawing = false;
        CreateArea(DrawPreview);
    }

    private void OnCanvasWheel(PointerEventInfo info)
    {
        var properties = info.Properties;
        var delta = properties.MouseWheelDelta;
        var horizontal = properties.IsHorizontalMouseWheel || info.Args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift);

        if (horizontal) Camera.X += properties.IsHorizontalMouseWheel ? delta : -delta;
        else Camera.Y -= delta;
        OnCameraChanged();
    }

    private async Task OnCanvasDoubleTapped(Point position)
    {
        if (!IsMapSelected || !IsEntityMode) return;

        var world = ToWorld(position);
        if (!IsInsideMap(world)) return;

        var entity = Entities.LastOrDefault(x => x.Contains(world.X, world.Y));
        if (entity is null)
        {
            await NewEntity(ToTile(world));
            return;
        }

        Select(entity);
        await EditEntity();
    }

    private void UpdateDrawPreview(Point tile)
    {
        if (_drawStart is not { } start) return;

        // inclusive of both tiles so a single click draws a one tile area
        var left = Math.Min(start.X, tile.X);
        var top = Math.Min(start.Y, tile.Y);
        var right = Math.Max(start.X, tile.X) + TileSize;
        var bottom = Math.Max(start.Y, tile.Y) + TileSize;
        DrawPreview = new Rect(left, top, right - left, bottom - top);
        IsDrawing = true;
    }

    private Point ToWorld(Point screen) => new(screen.X + Camera.X, screen.Y + Camera.Y);

    private Point ToTile(Point world)
    {
        var x = Math.Clamp(Calculations.RoundToMultiple(world.X, TileSize, Rounding.Down), 0, Math.Max(0, MapPixelWidth - TileSize));
        var y = Math.Clamp(Calculations.RoundToMultiple(world.Y, TileSize, Rounding.Down), 0, Math.Max(0, MapPixelHeight - TileSize));
        return new Point(x, y);
    }

    private bool IsInsideMap(Point world) => world.X >= 0 && world.Y >= 0 && world.X < MapPixelWidth && world.Y < MapPixelHeight;

    #endregion

    #region Entities

    private async Task NewEntity(Point tile)
    {
        if (!IsMapSelected || SelectedMap is not { } map) return;

        var id = Calculations.GetNextId([.. map.Entities.Select(x => x.Id)]);
        var entity = new Entity { Id = id, Name = $"Entity {id:000}", MapId = map.Guid };
        var page = new Page { Id = 1, Name = "Page 1", EntityId = entity.Guid };
        page.Modules.Add(new PositionModule { X = tile.X, Y = tile.Y });
        entity.Pages.Add(page);
        map.Entities.Add(entity);

        var model = new MapEntityModel(entity, TileSize);
        Entities.Add(model);
        Select(model);
        await EditEntity();
    }

    private async Task EditEntity()
    {
        if (SelectedEntity is not { } model) return;

        var viewModel = new EntityEditorViewModel(model.Entity, gameDataService, jsonService, navigationService);
        await navigationService.ShowEntityEditorPopup(viewModel);

        model.Refresh();
        OnPropertyChanged(nameof(EntityName));
        OnPropertyChanged(nameof(EntityZ));
        await LoadSprite(model);
        RebuildScene();
    }

    private void CopySelection()
    {
        _copiedEntity = SelectedEntity?.Entity;
        PasteSelectionCommand.RaiseCanExecuteChanged();
    }

    private void PasteSelection()
    {
        if (!IsMapSelected || SelectedMap is not { } map || _copiedEntity is null) return;
        if (jsonService.Clone(_copiedEntity) is not { } clone) return;

        ReassignGuids(clone, map.Guid);
        clone.Id = Calculations.GetNextId([.. map.Entities.Select(x => x.Id)]);
        map.Entities.Add(clone);

        var model = new MapEntityModel(clone, TileSize);
        model.MoveTo(_contextTile.X, _contextTile.Y);
        Entities.Add(model);
        Select(model);
        _ = RefreshEntitySprites();
    }

    private void DeleteSelection()
    {
        if (SelectedMap is not { } map) return;

        if (SelectedEntity is { } entity)
        {
            map.Entities.Remove(entity.Entity);
            Entities.Remove(entity);
            Select(null);
            RebuildScene();
            return;
        }

        if (SelectedArea is { } area)
        {
            map.Areas.Remove(area.Area);
            Areas.Remove(area);
            Select(null);
        }
    }

    private static void ReassignGuids(Entity entity, Guid mapGuid)
    {
        entity.Guid = Guid.NewGuid();
        entity.MapId = mapGuid;
        foreach (var page in entity.Pages)
        {
            page.Guid = Guid.NewGuid();
            page.EntityId = entity.Guid;
        }
    }

    #endregion

    #region Areas

    private void CreateArea(Rect bounds)
    {
        if (SelectedMap is not { } map || bounds.Width <= 0 || bounds.Height <= 0) return;

        var id = Calculations.GetNextId([.. map.Areas.Select(x => x.Id)]);
        var area = new MapArea
        {
            Id = id,
            Name = NewAreaType == MapAreaType.Area ? $"Area {id:000}" : string.Empty,
            AreaType = NewAreaType,
            OffsetX = (int) bounds.X,
            OffsetY = (int) bounds.Y,
            Width = (int) bounds.Width,
            Height = (int) bounds.Height
        };

        map.Areas.Add(area);
        var model = new MapAreaModel(area, TileSize);
        Areas.Add(model);
        Select(model);
    }

    #endregion

    #region Selection

    private void Select(object? item)
    {
        SelectedEntity = item as MapEntityModel;
        SelectedArea = item as MapAreaModel;
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(HasNoSelection));
        EditEntityCommand.RaiseCanExecuteChanged();
        CopySelectionCommand.RaiseCanExecuteChanged();
        DeleteSelectionCommand.RaiseCanExecuteChanged();
        NewEntityCommand.RaiseCanExecuteChanged();
        PasteSelectionCommand.RaiseCanExecuteChanged();
    }

    #endregion

    #region Audio

    private void PlayBgm()
    {
        if (SelectedMap?.Bgm is not { Name: { } name } bgm || name.IsNullOrEmpty()) return;
        audioService.PlayBgm(name, bgm.Volume, bgm.Pitch);
    }

    private void PlayBgs()
    {
        if (SelectedMap?.Bgs is not { Name: { } name } bgs || name.IsNullOrEmpty()) return;
        audioService.PlayBgs(name, bgs.Volume, bgs.Pitch);
    }

    private void StopAudio()
    {
        audioService.StopBgm();
        audioService.StopBgs();
    }

    private static AudioFile? ToAudioFile(AudioFile? current, string name, string path)
    {
        if (name == None || name.IsNullOrEmpty()) return null;
        current ??= new AudioFile { Path = path };
        current.Name = name;
        return current;
    }

    private static string[] GetFiles(string path)
    {
        return Directory.Exists(path) ? [.. Directory.GetFiles(path).Select(Path.GetFileName).OfType<string>()] : [];
    }

    #endregion
}
