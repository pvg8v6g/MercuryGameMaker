using GameMaker.UX.ViewModels.Popups.FillProps;
using GameLibrary.Models.Areas;
using GameLibrary.Configuration;
using GameLibrary.Utilities;
﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation;
using Windows.System;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Services.Media;
using GameMaker.Services.Maps;
using GameMaker.Services.Navigation;
using GameMaker.Services.Testing;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using GameMaker.UX.ViewModels.MainPage.Tools;
using GameMaker.UX.ViewModels.Popups.GameStart;
using GameMaker.UX.ViewModels.Popups.MapSettings;
using MercuryLibrary.Extensions;
using MercuryLibrary.ImageComponents;
using ContentDialogResult = Microsoft.UI.Xaml.Controls.ContentDialogResult;

namespace GameMaker.UX.ViewModels.MainPage;

/// <summary>
/// The map editor: the map tree, the selected map's settings, layers and audio, and the canvas.
/// What a click or a shortcut does on the canvas is up to the tool of the edit mode (EntityTool, AreaTool, PropTool).
/// </summary>
public partial class MainPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    IMediaService mediaService,
    IAudioService audioService,
    INavigationService navigationService,
    IMapEditService mapEditService,
    IPropFillService propFillService,
    ITestLaunchService testLaunchService) : BaseViewModel
{
    #region Fields

    private readonly Dictionary<Guid, MapTreeNode> _nodes = [];
    private bool _rebuildingTree;

    // canvas state
    private Point? _panStart;
    private Point _panCamera;
    private Point _contextTile;
    private Point _lastWorld;

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
            OnSelectedMapChanged().Forget();
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

    #region Selected Map

    // the selected map's id, size, parent, connections, images and music (the map settings and layers popups edit these)
    public SelectedMapViewModel Map => field ??= new SelectedMapViewModel(this, gameDataService, graphicsService, locationService, mediaService, audioService, mapEditService);

    #endregion

    #region Tools

    public EntityTool EntityTool => field ??= new EntityTool(this, gameDataService, jsonService, graphicsService, locationService, navigationService, mapEditService);

    public AreaTool AreaTool => field ??= new AreaTool(this);

    public PropTool PropTool => field ??= new PropTool(this, gameDataService, graphicsService, locationService);

    public PathTool PathTool => field ??= new PathTool(this);

    private IMapTool ActiveTool => EditMode switch
    {
        MapEditMode.Areas => AreaTool,
        MapEditMode.Props => PropTool,
        MapEditMode.Paths => PathTool,
        _ => EntityTool
    };

    public MapEditMode EditMode
    {
        get;
        set
        {
            if (field == value) return;
            SetField(ref field, value);
            OnPropertyChanged(nameof(IsEntityMode));
            OnPropertyChanged(nameof(IsAreaMode));
            OnPropertyChanged(nameof(IsPropMode));
            OnPropertyChanged(nameof(IsPathMode));
            OnPropertyChanged(nameof(PathsVisible));
            DeleteSelectionCommand.RaiseCanExecuteChanged();
            EditSelectionCommand.RaiseCanExecuteChanged();
            ApplyFocus();
            PropTool.OnModeChanged();
            Select(null);
            OnSnapSizeChanged();
            PasteSelectionCommand.RaiseCanExecuteChanged();
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

    // props mode needs something to stamp: off until props are made on the Props page
    public bool HasProps => PropTool.Choices.Count > 0;

    public bool IsPropMode
    {
        get => EditMode == MapEditMode.Props;
        set
        {
            if (value) EditMode = MapEditMode.Props;
        }
    }

    public bool IsPathMode
    {
        get => EditMode == MapEditMode.Paths;
        set
        {
            if (value) EditMode = MapEditMode.Paths;
        }
    }

    // what the cursor snaps to in the current mode, the grid and the hover box follow it
    public int SnapSize => ActiveTool.SnapSize;

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

    public bool ShowPaths
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(PathsVisible));
        }
    } = true;

    // patrol paths are always drawn while drawing them
    public bool PathsVisible => ShowPaths || IsPathMode;

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

    public bool ShowEnvironments
    {
        get;
        set
        {
            SetField(ref field, value);
            RebuildScene();
        }
    } = true;

    // what a test opens in and draws over the game: global settings kept in config.json, the engine reads the overlays from there
    // a test opens in a window unless full screen is picked (full screen tears on some graphics drivers)
    public bool TestFullScreen
    {
        get => locationService.Configuration?.TestFullScreen ?? false;
        set => SetTestSetting(value, TestFullScreen, configuration => configuration.TestFullScreen = value);
    }

    public bool TestShowHitboxes
    {
        get => locationService.Configuration?.ShowEntityHitboxes ?? false;
        set => SetTestSetting(value, TestShowHitboxes, configuration => configuration.ShowEntityHitboxes = value);
    }

    public bool TestShowAreas
    {
        get => locationService.Configuration?.ShowAreas ?? false;
        set => SetTestSetting(value, TestShowAreas, configuration => configuration.ShowAreas = value);
    }

    public bool TestShowCollisions
    {
        get => locationService.Configuration?.ShowCollisions ?? false;
        set => SetTestSetting(value, TestShowCollisions, configuration => configuration.ShowCollisions = value);
    }

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

    public string CursorText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    #endregion

    #region Audio Properties

    // footstep sounds are picked for the selected footstep area
    public string[] FootstepFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [AudioFiles.None];

    public string? SelectedFootstep
    {
        get => AudioFiles.NameOf(SelectedArea?.Area.Footstep);
        set
        {
            if (value is null || SelectedArea is null) return;
            SelectedArea.Area.Footstep = AudioFiles.FromName(SelectedArea.Area.Footstep, value, "Footsteps");
        }
    }

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

    public MapPropModel? SelectedProp
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

    public AreaColorOption[] AreaColors => AreaColorOption.All;

    // how solid what the current tool doesn't work on is drawn
    private const double UnfocusedOpacity = 0.3;

    public double AreasOpacity => IsAreaMode ? 1.0 : UnfocusedOpacity;

    public double PropsOverlayOpacity => IsPropMode ? 1.0 : UnfocusedOpacity;

    public double PathsOpacity => IsPathMode ? 1.0 : UnfocusedOpacity;

    // the side panels fold away to a small arrow, kept in config.json. The arrow points the way the panel will go
    public bool IsToolRailExpanded => !(locationService.Configuration?.ToolRailCollapsed ?? false);

    public string ToolRailGlyph => IsToolRailExpanded ? "" : "";

    public bool IsMapsPanelExpanded => !(locationService.Configuration?.MapsPanelCollapsed ?? false);

    public string MapsPanelGlyph => IsMapsPanelExpanded ? "" : "";

    public bool HasNoSelection => SelectedEntity is null && SelectedArea is null && SelectedProp is null;

    // the status bar: what's selected and the grid the cursor snaps to
    public string SelectionText => SelectedEntity is { } entity ? $"Entity: {entity.Name}"
        : SelectedArea is { } area ? $"Area: {area.Label}"
        : SelectedProp is { } prop ? $"Prop: {prop.Prop.Name}"
        : "Nothing selected";

    public string SnapText => $"Grid {SnapSize} px";

    #endregion

    #region Commands

    public AsyncRelayCommand NewMapCommand => field ??= new AsyncRelayCommand(() => NewMap(false));

    public AsyncRelayCommand NewFolderCommand => field ??= new AsyncRelayCommand(() => NewMap(true));

    public AsyncRelayCommand EditMapCommand => field ??= new AsyncRelayCommand(EditMap, () => SelectedMap is not null);

    public AsyncRelayCommand LayersCommand => field ??= new AsyncRelayCommand(() => navigationService.ShowMapLayersPopup(this), () => IsMapSelected);

    public RelayCommand CopyMapCommand => field ??= new RelayCommand(() => CopiedMap = SelectedMap, () => SelectedMap is not null);

    public AsyncRelayCommand PasteMapCommand => field ??= new AsyncRelayCommand(PasteMap);

    public RelayCommand DeleteMapCommand => field ??= new RelayCommand(DeleteMap, () => SelectedMap is not null);

    public RelayCommand<Size> CanvasSizeChangedCommand => field ??= new RelayCommand<Size>(OnCanvasSizeChanged);

    public RelayCommand<PointerEventInfo> CanvasPressedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasPressed);

    public RelayCommand<PointerEventInfo> CanvasMovedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasMoved);

    public RelayCommand<PointerEventInfo> CanvasReleasedCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasReleased);

    public RelayCommand<PointerEventInfo> CanvasWheelCommand => field ??= new RelayCommand<PointerEventInfo>(OnCanvasWheel);

    public AsyncRelayCommand<Point> CanvasDoubleTappedCommand => field ??= new AsyncRelayCommand<Point>(OnCanvasDoubleTapped);

    public AsyncRelayCommand NewEntityCommand => field ??= new AsyncRelayCommand(() => EntityTool.NewEntity(_contextTile), () => IsMapSelected && IsEntityMode);

    public AsyncRelayCommand SetGameStartCommand => field ??= new AsyncRelayCommand(SetGameStart, () => IsMapSelected);

    public RelayCommand SetTestSpawnCommand => field ??= new RelayCommand(SetTestSpawn, () => IsMapSelected);

    public AsyncRelayCommand TestMapCommand => field ??= new AsyncRelayCommand(TestMap, () => IsMapSelected && testLaunchService.CanLaunch);

    public AsyncRelayCommand EditSelectionCommand => field ??= new AsyncRelayCommand(EditSelection, () => !HasNoSelection || IsPathMode);

    public RelayCommand CopySelectionCommand => field ??= new RelayCommand(CopySelection, () => !HasNoSelection);

    public RelayCommand PasteSelectionCommand => field ??= new RelayCommand(() => ActiveTool.Paste(_lastWorld), () => IsMapSelected && ActiveTool.CanPaste);

    // the paths tool keeps its own picked point
    public RelayCommand DeleteSelectionCommand => field ??= new RelayCommand(() => ActiveTool.Delete(), () => !HasNoSelection || IsPathMode);

    public RelayCommand ClearSelectionCommand => field ??= new RelayCommand(() => Select(null));

    public RelayCommand ToggleToolRailCommand => field ??= new RelayCommand(ToggleToolRail);

    public RelayCommand ToggleMapsPanelCommand => field ??= new RelayCommand(ToggleMapsPanel);

    public AsyncRelayCommand FillPropsCommand => field ??= new AsyncRelayCommand(FillProps, () => IsMapSelected);

    public AsyncRelayCommand ClearPropsCommand => field ??= new AsyncRelayCommand(ClearProps, () => IsMapSelected);

    public AsyncRelayCommand ImportPropsCommand => field ??= new AsyncRelayCommand(ImportProps, () => IsMapSelected);

    public RelayCommand FlipSelectionCommand => field ??= new RelayCommand(() => PropTool.FlipSelected(), () => SelectedProp is not null);

    // "Left", "Right", "Up", "Down", with "Shift+" for a whole tile
    public RelayCommand<string> NudgeSelectionCommand => field ??= new RelayCommand<string>(NudgeSelection);


    #endregion

    #region Loading

    protected override async Task LoadedAction()
    {
        // rebuilt every visit so newly added graphics and media show up
        Map.RefreshMediaFiles();
        FootstepFiles = [AudioFiles.None, .. mediaService.GetMediaFiles(Cache.Footsteps)];

        var selected = SelectedMap?.Guid ?? gameDataService.GameMaps.OrderBy(x => x.Id).FirstOrDefault(x => !x.FolderOnly)?.Guid;
        RebuildTree(selected);
        RefreshMapState();

        // props and entities may have been edited elsewhere (the props catalogue, the character image of a page)
        await PropTool.LoadChoices();
        OnPropertyChanged(nameof(HasProps));
        if (!HasProps && IsPropMode) EditMode = MapEditMode.Entities;
        await PropTool.LoadMapProps();
        await EntityTool.LoadSprites();
    }

    private async Task OnSelectedMapChanged()
    {
        Select(null);
        RefreshMapState();
        RebuildGrid();
        RebuildModels();

        Camera.X = 0;
        Camera.Y = 0;
        OnCameraChanged();

        await PropTool.LoadMapProps();
        await EntityTool.LoadSprites();
    }

    #endregion

    #region Map Tree

    private async Task NewMap(bool folder)
    {
        var parent = SelectedMap;
        parent?.IsExpanded = true;
        var map = mapEditService.Create(folder, parent?.Guid);
        RebuildTree(map.Guid);

        var result = await navigationService.ShowMapSettingsPopup(new MapSettingsViewModel(this, true));
        Map.StopAudio();
        if (result != ContentDialogResult.Primary && Equals(SelectedMap, map)) DeleteMap();
    }

    private async Task EditMap()
    {
        if (SelectedMap is null) return;
        await navigationService.ShowMapSettingsPopup(new MapSettingsViewModel(this, false));
        Map.StopAudio();
    }

    // the copy takes the id after the original (40 pastes as 41), a map already on that id asks which one moves
    private async Task PasteMap()
    {
        if (CopiedMap is null || mapEditService.Duplicate(CopiedMap) is not { } clone) return;
        RebuildTree(clone.Guid);

        if (await mapEditService.ResolveIdConflict(clone))
        {
            RebuildTree(clone.Guid);
            RefreshMapState();
            return;
        }

        // cancelling the conflict cancels the paste
        gameDataService.GameMaps.Remove(clone);
        RebuildTree(CopiedMap.Guid);
    }

    // children move up to the deleted map's parent so deleting a folder never deletes the maps inside it
    private void DeleteMap()
    {
        if (SelectedMap is not { } map) return;

        mapEditService.Delete(map);
        if (Equals(CopiedMap, map)) CopiedMap = null;
        RebuildTree(map.ParentGuid ?? gameDataService.GameMaps.OrderBy(x => x.Id).FirstOrDefault()?.Guid);
    }

    private void RebuildTree(Guid? select)
    {
        _rebuildingTree = true;
        MapTreeBuilder.Build(gameDataService.GameMaps, _nodes, MapTree);
        _rebuildingTree = false;

        MapTreeNode? selected = null;
        if (select is { } guid) _nodes.TryGetValue(guid, out selected);
        selected ??= MapTree.FirstOrDefault();
        MapTreeBuilder.ExpandParents(selected, _nodes);

        // the node objects are new, the same map keeps its state and the tree selects the new instance
        SelectedNode = selected;
        Map.RefreshOptions();
        CopyMapCommand.RaiseCanExecuteChanged();
        EditMapCommand.RaiseCanExecuteChanged();
        DeleteMapCommand.RaiseCanExecuteChanged();
    }

    #endregion

    #region Map Changes

    // what the editor shows for the selected map besides its settings: commands, spawns, sizes
    private void RefreshMapState()
    {
        Map.Refresh();
        OnPropertyChanged(nameof(IsMapSelected));
        OnPropertyChanged(nameof(IsFolderSelected));
        OnPropertyChanged(nameof(MapPixelWidth));
        OnPropertyChanged(nameof(MapPixelHeight));
        NewEntityCommand.RaiseCanExecuteChanged();
        PasteSelectionCommand.RaiseCanExecuteChanged();
        LayersCommand.RaiseCanExecuteChanged();
        SetGameStartCommand.RaiseCanExecuteChanged();
        SetTestSpawnCommand.RaiseCanExecuteChanged();
        TestMapCommand.RaiseCanExecuteChanged();
        ImportPropsCommand.RaiseCanExecuteChanged();
        FillPropsCommand.RaiseCanExecuteChanged();
        ClearPropsCommand.RaiseCanExecuteChanged();
        RefreshSpawns();
    }

    public void OnMapSizeChanged()
    {
        OnPropertyChanged(nameof(MapPixelWidth));
        OnPropertyChanged(nameof(MapPixelHeight));
        RebuildGrid();
        RebuildScene(); // environments are tiled to the map size
        OnCameraChanged();
    }

    // the map moved into another folder: open that folder and rebuild the tree around it
    public void OnParentChanged(Guid? parent)
    {
        if (parent is { } guid && _nodes.TryGetValue(guid, out var parentNode)) parentNode.IsExpanded = true;
        RebuildTree(SelectedMap?.Guid);
    }

    // the tree and the map options are sorted and labeled by id, and the map images are found by it
    public void OnMapIdChanged()
    {
        RebuildTree(SelectedMap?.Guid);
        RefreshMapState();
    }

    private void ToggleToolRail()
    {
        if (locationService.Configuration is not { } configuration) return;

        configuration.ToolRailCollapsed = !configuration.ToolRailCollapsed;
        locationService.SaveConfiguration();
        OnPropertyChanged(nameof(IsToolRailExpanded));
        OnPropertyChanged(nameof(ToolRailGlyph));
    }

    private void ToggleMapsPanel()
    {
        if (locationService.Configuration is not { } configuration) return;

        configuration.MapsPanelCollapsed = !configuration.MapsPanelCollapsed;
        locationService.SaveConfiguration();
        OnPropertyChanged(nameof(IsMapsPanelExpanded));
        OnPropertyChanged(nameof(MapsPanelGlyph));
    }

    private void SetTestSetting(bool value, bool current, Action<MercuryGameMakerConfiguration> apply)
    {
        if (value == current || locationService.Configuration is not { } configuration) return;
        apply(configuration);
        locationService.SaveConfiguration();
        OnPropertyChanged(nameof(TestFullScreen));
        OnPropertyChanged(nameof(TestShowHitboxes));
        OnPropertyChanged(nameof(TestShowAreas));
        OnPropertyChanged(nameof(TestShowCollisions));
    }

    private bool IsLayerShown(MapLayer layer) => layer.Type switch
    {
        MapLayerType.Parallax => ShowParallax,
        MapLayerType.Light => ShowLight,
        _ => true
    };

    #endregion

    #region Scene

    // the scene sorts by z, so layers, characters and props interleave however their z values say
    public void RebuildScene()
    {
        Scene.Clear();
        if (IsMapSelected)
        {
            MapSceneBuilder.Build(
                Scene,
                Map.Layers.Where(x => IsLayerShown(x.Layer)),
                ShowEnvironments ? Map.Environments.Where(x => x.IsOn) : [],
                Entities,
                PropTool.Props,
                MapPixelWidth,
                MapPixelHeight,
                TileSize);
        }

        PropTool.RefreshFootprints();
        ApplyFocus();
        Scene.ApplyCamera(Camera);
    }

    // what the current tool works on is drawn as it is, everything else on the map fades back a little so the
    // tool's things stand out. The map images themselves never fade
    private void ApplyFocus()
    {
        foreach (var entity in Entities) entity.Picture?.Opacity = IsEntityMode ? 1.0f : (float) UnfocusedOpacity;
        foreach (var prop in PropTool.Props) prop.Picture?.Opacity = IsPropMode ? 1.0f : (float) UnfocusedOpacity;

        OnPropertyChanged(nameof(AreasOpacity));
        OnPropertyChanged(nameof(PropsOverlayOpacity));
        OnPropertyChanged(nameof(PathsOpacity));
        PathTool.Refresh();
    }

    private void RebuildModels()
    {
        Entities.Clear();
        Areas.Clear();
        if (!IsMapSelected || SelectedMap is not { } map)
        {
            PathTool.Load(null);
            return;
        }

        foreach (var entity in map.Entities) Entities.Add(new MapEntityModel(entity, TileSize));
        foreach (var area in map.Areas) Areas.Add(new MapAreaModel(area, AreaTool.GridSize));
        PathTool.Load(map);
    }

    // the grid shows what the cursor snaps to
    private void RebuildGrid()
    {
        GridLines.Clear();
        if (!IsMapSelected) return;

        // props sit on the 4 px character step grid, too fine to draw, so the tile grid shows instead
        var size = IsPropMode ? TileSize : SnapSize;
        for (var x = 0; x <= MapPixelWidth; x += size) GridLines.Add(new GridLine(x, 0, x, MapPixelHeight));
        for (var y = 0; y <= MapPixelHeight; y += size) GridLines.Add(new GridLine(0, y, MapPixelWidth, y));
    }

    public void OnSnapSizeChanged()
    {
        OnPropertyChanged(nameof(SnapSize));
        OnPropertyChanged(nameof(SnapText));
        RebuildGrid();
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
        if (info.Properties.IsMiddleButtonPressed)
        {
            _panStart = info.Position;
            _panCamera = new Point(Camera.X, Camera.Y);
            return;
        }

        if (!IsMapSelected) return;

        var world = EditorGrid.ToWorld(info.Position, Camera);
        if (!EditorGrid.IsInside(world, MapPixelWidth, MapPixelHeight))
        {
            Select(null);
            return;
        }

        _contextTile = EditorGrid.ToCell(world, TileSize, MapPixelWidth, MapPixelHeight);
        _lastWorld = world;
        ActiveTool.Pressed(world, info.Properties.IsLeftButtonPressed);
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

        var world = EditorGrid.ToWorld(info.Position, Camera);
        var inside = EditorGrid.IsInside(world, MapPixelWidth, MapPixelHeight);
        _lastWorld = world;

        // the hover box marks the cell the cursor snaps to, props show their picture instead
        IsHovering = inside && !IsPropMode;
        if (inside)
        {
            var cell = EditorGrid.ToCell(world, SnapSize, MapPixelWidth, MapPixelHeight);
            HoverX = cell.X;
            HoverY = cell.Y;
            CursorText = ActiveTool.CursorText(world);
            _contextTile = EditorGrid.ToCell(world, TileSize, MapPixelWidth, MapPixelHeight);
        }

        ActiveTool.Moved(world, inside);
    }

    private void OnCanvasReleased(PointerEventInfo info)
    {
        _panStart = null;
        ActiveTool.Released();
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

    // double-clicking opens the editor of what's under the cursor, whichever tool is in use
    private async Task OnCanvasDoubleTapped(Point position)
    {
        if (!IsMapSelected) return;

        var world = EditorGrid.ToWorld(position, Camera);
        if (EditorGrid.IsInside(world, MapPixelWidth, MapPixelHeight)) await ActiveTool.DoubleTapped(world);
    }

    #endregion

    #region Selection

    public void Select(object? item)
    {
        SelectedEntity = item as MapEntityModel;
        SelectedArea = item as MapAreaModel;
        SelectedProp = item as MapPropModel;
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(HasNoSelection));
        OnPropertyChanged(nameof(SelectionText));
        EditSelectionCommand.RaiseCanExecuteChanged();
        CopySelectionCommand.RaiseCanExecuteChanged();
        DeleteSelectionCommand.RaiseCanExecuteChanged();
        NewEntityCommand.RaiseCanExecuteChanged();
        PasteSelectionCommand.RaiseCanExecuteChanged();
        FlipSelectionCommand.RaiseCanExecuteChanged();
    }

    public async Task EditSelection()
    {
        if (SelectedEntity is not null) await EntityTool.EditSelected();
        else if (SelectedArea is not null) await navigationService.ShowAreaEditorPopup(this);
        else if (SelectedProp is not null) await navigationService.ShowPropEditorPopup(this);
        else if (IsPathMode && PathTool.HasSelectedPath) await navigationService.ShowPathEditorPopup(this);
    }

    // fills the map with props by a fill set (Forest, Desert, Snow), made and edited in the popup
    private async Task FillProps()
    {
        if (SelectedMap is not { } map) return;

        var areaNames = map.Areas.Where(x => x.AreaType == MapAreaType.Area).Select(x => x.Name);
        var viewModel = new FillPropsViewModel(gameDataService, PropTool.Choices, areaNames, map.Name);
        if (await navigationService.ShowFillPropsPopup(viewModel) != ContentDialogResult.Primary || viewModel.SelectedFillSet is not { } fillSet) return;

        if (viewModel.ClearFirst) PropTool.ClearAll();
        var placed = await propFillService.Fill(map, fillSet, (int) viewModel.Seed, map.Props);
        await PropTool.AddMany(placed);
        CursorText = $"Filled {placed.Count} props";
    }

    private async Task ClearProps()
    {
        if (SelectedMap is not { } map || map.Props.Count == 0) return;
        if (!await navigationService.Confirm("Clear Props", $"Remove all {map.Props.Count} props from {map.Name}?", "Clear")) return;
        PropTool.ClearAll();
    }

    private async Task ImportProps()
    {
        if (await navigationService.PickFile(".json") is not { } path) return;
        await navigationService.ShowMessage("Import Props", await PropTool.Import(path));
    }

    private void CopySelection()
    {
        if (ActiveTool.Copy()) PasteSelectionCommand.RaiseCanExecuteChanged();
    }

    private void NudgeSelection(string direction)
    {
        var big = direction.StartsWith("Shift+");
        var (dx, dy) = (big ? direction["Shift+".Length..] : direction) switch
        {
            "Left" => (-1, 0),
            "Right" => (1, 0),
            "Up" => (0, -1),
            "Down" => (0, 1),
            _ => (0, 0)
        };
        if (dx != 0 || dy != 0) ActiveTool.Nudge(dx, dy, big);
    }

    #endregion

    #region Spawns

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

    private async Task TestMap()
    {
        if (SelectedMap is not { } map) return;

        // the game plays the map's audio itself, a preview left on here would play over it
        Map.StopAudio();
        await testLaunchService.Launch(map, !TestFullScreen);
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

}
