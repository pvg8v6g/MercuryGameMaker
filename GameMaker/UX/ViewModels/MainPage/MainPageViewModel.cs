using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using GameMaker.Tasks;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using GameMaker.UX.ViewModels.Popups.GameStart;
using GameMaker.UX.ViewModels.Popups.MapIdConflict;
using GameMaker.UX.ViewModels.Popups.MapSettings;
using MercuryLibrary.Extensions;
using MercuryLibrary.ImageComponents;
using Page = GameLibrary.Models.Page;
using ContentDialogResult = Microsoft.UI.Xaml.Controls.ContentDialogResult;

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

    // the selected map's id, its images are found by id so a clash opens the conflict popup
    public MapIdModel? SelectedMapId
    {
        get;
        private set => SetField(ref field, value);
    }

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

    // the selected map's own images, in draw order
    public MapLayerModel[] Layers
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    // everything in Graphics\Maps\Environment, each one turned on or off for the selected map
    public MapEnvironmentModel[] Environments
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
            OnPropertyChanged(nameof(SnapSize));
            Select(null);
            RebuildGrid();
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

    // areas snap to this grid, smaller than a tile for finer collisions (every size divides the 48 pixel tile)
    public int[] AreaGridSizes { get; } = [48, 24, 16, 12, 8, 6, 4];

    public int AreaGridSize
    {
        get;
        set
        {
            if (value <= 0 || field == value) return;
            SetField(ref field, value);
            foreach (var area in Areas) area.CellSize = value;
            OnPropertyChanged(nameof(SnapSize));
            RebuildGrid();
        }
    } = 48;

    // what the cursor snaps to: areas use the area grid, everything else the tile
    public int SnapSize => IsAreaMode ? AreaGridSize : TileSize;

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

    // tests in a window instead of full screen (full screen tears on some graphics drivers)
    public bool TestWindowed
    {
        get;
        set => SetField(ref field, value);
    }

    // what a test draws over the game: global settings kept in config.json, the engine reads them from there
    public bool TestShowHitboxes
    {
        get => locationService.Configuration?.ShowEntityHitboxes ?? false;
        set
        {
            if (value == TestShowHitboxes || locationService.Configuration is not { } configuration) return;
            configuration.ShowEntityHitboxes = value;
            locationService.SaveConfiguration();
            OnPropertyChanged();
        }
    }

    public bool TestShowAreas
    {
        get => locationService.Configuration?.ShowAreas ?? false;
        set
        {
            if (value == TestShowAreas || locationService.Configuration is not { } configuration) return;
            configuration.ShowAreas = value;
            locationService.SaveConfiguration();
            OnPropertyChanged();
        }
    }

    public bool TestShowCollisions
    {
        get => locationService.Configuration?.ShowCollisions ?? false;
        set
        {
            if (value == TestShowCollisions || locationService.Configuration is not { } configuration) return;
            configuration.ShowCollisions = value;
            locationService.SaveConfiguration();
            OnPropertyChanged();
        }
    }

    public bool ShowEnvironments
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

    #region Spawn Properties

    // the game start and the test spawn are drawn on the map they belong to
    public bool IsGameStartShown => SelectedMap is { } map && gameDataService.GameStart.MapGuid == map.Guid;

    public double GameStartX => gameDataService.GameStart.Spawn.TileX * TileSize;

    public double GameStartY => gameDataService.GameStart.Spawn.TileY * TileSize;

    public bool IsTestSpawnShown => SelectedMap?.TestSpawn is not null;

    public double TestSpawnX => (SelectedMap?.TestSpawn?.TileX ?? 0) * TileSize;

    public double TestSpawnY => (SelectedMap?.TestSpawn?.TileY ?? 0) * TileSize;

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

    #endregion

    #region Commands

    public AsyncRelayCommand NewMapCommand => field ??= new AsyncRelayCommand(() => NewMap(false));

    public AsyncRelayCommand NewFolderCommand => field ??= new AsyncRelayCommand(() => NewMap(true));

    public AsyncRelayCommand EditMapCommand => field ??= new AsyncRelayCommand(EditMap, () => SelectedMap is not null);

    public AsyncRelayCommand LayersCommand => field ??= new AsyncRelayCommand(() => navigationService.ShowMapLayersPopup(this), () => IsMapSelected);

    public RelayCommand CopyMapCommand => field ??= new RelayCommand(CopyMap, () => SelectedMap is not null);

    public AsyncRelayCommand PasteMapCommand => field ??= new AsyncRelayCommand(PasteMap);

    public RelayCommand DeleteMapCommand => field ??= new RelayCommand(DeleteMap, () => SelectedMap is not null);

    public AsyncRelayCommand FitToGroundCommand => field ??= new AsyncRelayCommand(FitToGround);

    public RelayCommand<Size> CanvasSizeChangedCommand => field ??= new RelayCommand<Size>(OnCanvasSizeChanged);

    public RelayCommand<PointerEventInfo> CanvasPressedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasPressed);

    public RelayCommand<PointerEventInfo> CanvasMovedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasMoved);

    public RelayCommand<PointerEventInfo> CanvasReleasedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasReleased);

    public RelayCommand<PointerEventInfo> CanvasWheelCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasWheel);

    public AsyncRelayCommand<Point> CanvasDoubleTappedCommand => field ??= new AsyncRelayCommand<Point>(OnCanvasDoubleTapped);

    public AsyncRelayCommand NewEntityCommand => field ??= new AsyncRelayCommand(() => NewEntity(_contextTile), () => IsMapSelected && IsEntityMode);

    public AsyncRelayCommand SetGameStartCommand => field ??= new AsyncRelayCommand(SetGameStart, () => IsMapSelected);

    public RelayCommand SetTestSpawnCommand => field ??= new RelayCommand(SetTestSpawn, () => IsMapSelected);

    public AsyncRelayCommand TestMapCommand => field ??= new AsyncRelayCommand(TestMap, () => IsMapSelected && File.Exists(EnginePath));

    public AsyncRelayCommand EditSelectionCommand => field ??= new AsyncRelayCommand(EditSelection, () => !HasNoSelection);

    public RelayCommand CopySelectionCommand => field ??= new RelayCommand(CopySelection, () => SelectedEntity is not null);

    public RelayCommand PasteSelectionCommand =>
        field ??= new RelayCommand(PasteSelection, () => _copiedEntity is not null && IsMapSelected && IsEntityMode);

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
        await RefreshMapSettings();

        // entities may have been edited elsewhere (e.g. the character image of a page)
        await RefreshEntitySprites();
    }

    private async Task OnSelectedMapChanged()
    {
        Select(null);
        await RefreshMapSettings();
        RebuildGrid();
        RebuildModels();

        Camera.X = 0;
        Camera.Y = 0;
        OnCameraChanged();

        await RefreshEntitySprites();
    }

    #endregion

    #region Map Tree

    private async Task NewMap(bool folder)
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

        var result = await navigationService.ShowMapSettingsPopup(new MapSettingsViewModel(this, true));
        StopAudio();
        if (result != ContentDialogResult.Primary && Equals(SelectedMap, map)) DeleteMap();
    }

    private async Task EditMap()
    {
        if (SelectedMap is null) return;
        await navigationService.ShowMapSettingsPopup(new MapSettingsViewModel(this, false));
        StopAudio();
    }

    private void CopyMap()
    {
        CopiedMap = SelectedMap;
    }

    // the copy takes the id after the original (40 pastes as 41), a map already on that id asks which one moves
    private async Task PasteMap()
    {
        if (CopiedMap is null || jsonService.Clone(CopiedMap) is not { } clone) return;

        clone.Guid = Guid.NewGuid();
        clone.Id = CopiedMap.Id + 1;
        clone.IsExpanded = false;
        foreach (var entity in clone.Entities) ReassignGuids(entity, clone.Guid);
        foreach (var area in clone.Areas) area.Guid = Guid.NewGuid();

        gameDataService.GameMaps.Add(clone);
        RebuildTree(clone.Guid);

        // cancelling the conflict cancels the paste
        if (await ResolveIdConflict(clone)) return;
        gameDataService.GameMaps.Remove(clone);
        RebuildTree(CopiedMap.Guid);
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
        if (Equals(CopiedMap, map)) CopiedMap = null;

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
        EditMapCommand.RaiseCanExecuteChanged();
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

    private Task RefreshMapSettings()
    {
        try
        {
            SelectedMapId = SelectedMap is { } selected ? new MapIdModel(selected, gameDataService.GameMaps, OnMapIdChanged) : null;
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
            LayersCommand.RaiseCanExecuteChanged();
            SetGameStartCommand.RaiseCanExecuteChanged();
            SetTestSpawnCommand.RaiseCanExecuteChanged();
            TestMapCommand.RaiseCanExecuteChanged();
            RefreshSpawns();
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    private void OnMapSizeChanged()
    {
        OnPropertyChanged(nameof(MapWidth));
        OnPropertyChanged(nameof(MapHeight));
        OnPropertyChanged(nameof(MapPixelWidth));
        OnPropertyChanged(nameof(MapPixelHeight));
        RebuildGrid();
        RebuildScene(); // environments are tiled to the map size
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

    private async Task OnMapIdChanged(MapIdModel changed, int previousId)
    {
        var map = changed.Map;
        if (!await ResolveIdConflict(map)) map.Id = previousId;

        // the tree and the map options are sorted and labeled by id, and the images are found by it
        RebuildTree(SelectedMap?.Guid);
        await RefreshMapSettings();
    }

    // when another map has the same id, the conflict popup stays up until every map has its own id again,
    // false when it was canceled (the other map's id is put back, the caller decides what happens to this one)
    private async Task<bool> ResolveIdConflict(GameMap map)
    {
        if (gameDataService.GameMaps.FirstOrDefault(x => !Equals(x, map) && x.Id == map.Id) is not { } other) return true;

        var otherId = other.Id;
        var viewModel = new MapIdConflictViewModel(new MapIdModel(map, gameDataService.GameMaps), new MapIdModel(other, gameDataService.GameMaps));
        while (true)
        {
            var result = await navigationService.ShowMapIdConflictPopup(viewModel);
            if (result != ContentDialogResult.Primary)
            {
                other.Id = otherId;
                return false;
            }

            if (viewModel.HasConflict) continue;
            RebuildTree(SelectedMap?.Guid);
            await RefreshMapSettings();
            return true;
        }
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

        RefreshEnvironments();
    }

    private void RefreshEnvironments()
    {
        foreach (var environment in Environments) environment.PropertyChanged -= OnLayerChanged;

        var graphicsDirectory = locationService.GraphicsDirectory!;
        Environments = SelectedMap is { FolderOnly: false } map
            ? [.. MapEnvironment.Names(graphicsDirectory).Select(name => new MapEnvironmentModel(map, name, graphicsDirectory))]
            : [];

        foreach (var environment in Environments) environment.PropertyChanged += OnLayerChanged;
        _ = LoadEnvironmentSizes(Environments);
    }

    // the frames are tiled over the map, so their size is needed before they can be drawn
    private async Task LoadEnvironmentSizes(MapEnvironmentModel[] environments)
    {
        foreach (var environment in environments.Where(x => x.FramePaths.Length > 0))
        {
            var (width, height) = await graphicsService.GetImageDimensions(environment.FramePaths[0]);
            environment.FrameSize = (width, height);
        }

        if (environments == Environments) RebuildScene();
    }

    private void OnLayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        // raised once for every change to an image's z, opacity or blend, and when an environment is turned on or off
        if (e.PropertyName is nameof(MapImageModel.IsDefault) or nameof(MapEnvironmentModel.IsOn)) RebuildScene();
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

        foreach (var layer in Layers.Where(x => IsLayerShown(x.Layer))) Scene.AddImage(layer.Layer, layer.ImagePath);
        if (ShowEnvironments)
        {
            foreach (var environment in Environments.Where(x => x.IsOn)) AddEnvironment(environment);
        }

        foreach (var entity in Entities)
        {
            entity.Picture = null;
            if (entity.Sprite is not { ImagePath: { } path, Rect: { } rect }) continue;
            var (x, y) = SpritePosition(entity, rect);
            entity.Picture = Scene.Add(path, x, y, entity.DrawZ, rect);
        }

        Scene.ApplyCamera(Camera);
    }

    // the first frame repeated until it covers the whole map
    private void AddEnvironment(MapEnvironmentModel environment)
    {
        var (width, height) = environment.FrameSize;
        if (environment.FramePaths.Length == 0 || width <= 0 || height <= 0) return;

        for (var y = 0.0; y < MapPixelHeight; y += height)
        {
            for (var x = 0.0; x < MapPixelWidth; x += width) Scene.AddImage(environment.Environment, environment.FramePaths[0], x, y);
        }
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
        foreach (var area in map.Areas) Areas.Add(new MapAreaModel(area, AreaGridSize));
    }

    private async Task RefreshEntitySprites()
    {
        var map = SelectedMap;
        await Task.WhenAll(Entities.ToArray().Select(LoadSprite));

        // the user may have picked another map while the sprites loaded
        if (Equals(map, SelectedMap)) RebuildScene();
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

        // the grid shows what the cursor snaps to
        for (var x = 0; x <= MapPixelWidth; x += SnapSize) GridLines.Add(new GridLine(x, 0, x, MapPixelHeight));
        for (var y = 0; y <= MapPixelHeight; y += SnapSize) GridLines.Add(new GridLine(0, y, MapPixelWidth, y));
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
        var cell = ToCell(world, AreaGridSize);
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
            _dragAreaOffset = new Point(cell.X - area.X, cell.Y - area.Y);
            return;
        }

        _drawStart = cell;
        UpdateDrawPreview(cell);
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
        var cell = ToCell(world, AreaGridSize);
        if (IsHovering)
        {
            var snapped = IsAreaMode ? cell : tile;
            HoverX = snapped.X;
            HoverY = snapped.Y;
            CursorText = IsAreaMode
                ? $"Cell {(int) (cell.X / AreaGridSize)}, {(int) (cell.Y / AreaGridSize)}"
                : $"Tile {(int) (tile.X / TileSize)}, {(int) (tile.Y / TileSize)}";
            _contextTile = tile;
        }

        // both are whole map pixels on the tile grid, compared as ints, so the check is exact
        if (_dragEntity is not null && ((int) _dragEntity.X != (int) tile.X || (int) _dragEntity.Y != (int) tile.Y))
        {
            _dragEntity.MoveTo(tile.X, tile.Y);
            if (_dragEntity.Picture is not { } picture || _dragEntity.Sprite?.Rect is not { } rect) return;
            var (x, y) = SpritePosition(_dragEntity, rect);
            Scene.Move(picture, x, y, Camera);
        }
        else if (_dragArea is not null)
        {
            var x = Math.Clamp(cell.X - _dragAreaOffset.X, 0, Math.Max(0, MapPixelWidth - _dragArea.Width));
            var y = Math.Clamp(cell.Y - _dragAreaOffset.Y, 0, Math.Max(0, MapPixelHeight - _dragArea.Height));
            _dragArea.MoveTo((int) x, (int) y);
        }
        else if (_drawStart is not null)
        {
            UpdateDrawPreview(cell);
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

    private void UpdateDrawPreview(Point cell)
    {
        if (_drawStart is not { } start) return;

        // inclusive of both cells so a single click draws a one cell area
        var left = Math.Min(start.X, cell.X);
        var top = Math.Min(start.Y, cell.Y);
        var right = Math.Max(start.X, cell.X) + AreaGridSize;
        var bottom = Math.Max(start.Y, cell.Y) + AreaGridSize;
        DrawPreview = new Rect(left, top, right - left, bottom - top);
        IsDrawing = true;
    }

    private Point ToWorld(Point screen) => new(screen.X + Camera.X, screen.Y + Camera.Y);

    private Point ToTile(Point world) => ToCell(world, TileSize);

    // the top left of the cell the point is in, kept inside the map
    private Point ToCell(Point world, int size)
    {
        var x = Math.Clamp(Calculations.RoundToMultiple(world.X, size, Rounding.Down), 0, Math.Max(0, MapPixelWidth - size));
        var y = Math.Clamp(Calculations.RoundToMultiple(world.Y, size, Rounding.Down), 0, Math.Max(0, MapPixelHeight - size));
        return new Point(x, y);
    }

    private bool IsInsideMap(Point world) => world is { X: >= 0, Y: >= 0 } && world.X < MapPixelWidth && world.Y < MapPixelHeight;

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

    private async Task EditSelection()
    {
        if (SelectedEntity is not null) await EditEntity();
        else if (SelectedArea is not null) await navigationService.ShowAreaEditorPopup(this);
    }

    private async Task EditEntity()
    {
        if (SelectedEntity is not { } model) return;

        var viewModel = new EntityEditorViewModel(model.Entity, gameDataService, jsonService, graphicsService, locationService, navigationService);
        await navigationService.ShowEntityEditorPopup(viewModel);

        model.Refresh();
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
        var model = new MapAreaModel(area, AreaGridSize);
        Areas.Add(model);
        Select(model);
    }

    #endregion

    #region Spawns

    // the engine is copied next to the editor when it builds (see BundleGameEngine in the project file)
    private static string EnginePath => Path.Combine(AppContext.BaseDirectory, "Engine", "GameEngine.exe");

    private async Task SetGameStart()
    {
        if (SelectedMap is not { } map) return;

        var start = gameDataService.GameStart;
        start.MapGuid = map.Guid;
        start.Spawn.TileX = (int) (_contextTile.X / TileSize);
        start.Spawn.TileY = (int) (_contextTile.Y / TileSize);
        RefreshSpawns();

        await navigationService.ShowGameStartPopup(new GameStartViewModel(start, gameDataService.Actors, map.Name));
    }

    private void SetTestSpawn()
    {
        if (SelectedMap is not { } map) return;

        map.TestSpawn ??= new SpawnPoint();
        map.TestSpawn.TileX = (int) (_contextTile.X / TileSize);
        map.TestSpawn.TileY = (int) (_contextTile.Y / TileSize);
        RefreshSpawns();
    }

    // the engine plays the saved game, so the game is saved before the map is opened in it
    private async Task TestMap()
    {
        if (SelectedMap is not { } map || !File.Exists(EnginePath)) return;

        // the game plays the map's audio itself, a preview left on here would play over it
        StopAudio();
        await navigationService.ShowProgressPopup<SaveDataTask>("Saving Game Files");

        var startInfo = new ProcessStartInfo(EnginePath)
        {
            WorkingDirectory = Path.GetDirectoryName(EnginePath),
            UseShellExecute = false
        };

        // the engine's StartService reads this, it opens the map at its test spawn
        startInfo.ArgumentList.Add("--test");
        startInfo.ArgumentList.Add(map.Guid.ToString());
        if (TestWindowed) startInfo.ArgumentList.Add("--windowed");
        Process.Start(startInfo);
    }

    private void RefreshSpawns()
    {
        OnPropertyChanged(nameof(IsGameStartShown));
        OnPropertyChanged(nameof(GameStartX));
        OnPropertyChanged(nameof(GameStartY));
        OnPropertyChanged(nameof(IsTestSpawnShown));
        OnPropertyChanged(nameof(TestSpawnX));
        OnPropertyChanged(nameof(TestSpawnY));
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
        EditSelectionCommand.RaiseCanExecuteChanged();
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
