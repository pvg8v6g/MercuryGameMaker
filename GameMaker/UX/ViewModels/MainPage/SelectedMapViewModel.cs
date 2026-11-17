using System.ComponentModel;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using GameLibrary.Services.Media;
using GameLibrary.Utilities;
using GameMaker.Services.Maps;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.MainPage;

/// <summary>
/// The selected map's own settings: id, size, parent folder, connections, images (layers and environments) and music.
/// The map settings and map layers popups edit these, the editor rebuilds its tree and canvas when they change.
/// </summary>
public partial class SelectedMapViewModel(
    MainPageViewModel editor,
    IGameDataService gameDataService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    IMediaService mediaService,
    IAudioService audioService,
    IMapEditService mapEditService) : PropertyChangedUpdater
{
    #region Properties

    private GameMap? Map => editor.SelectedMap;

    // the selected map's id, its images are found by id so a clash opens the conflict popup
    public MapIdModel? SelectedMapId
    {
        get;
        private set => SetField(ref field, value);
    }

    public int MapWidth
    {
        get => Map?.Width ?? 0;
        set
        {
            if (Map is null || value <= 0 || Map.Width == value) return;
            Map.Width = value;
            OnSizeChanged();
        }
    }

    public int MapHeight
    {
        get => Map?.Height ?? 0;
        set
        {
            if (Map is null || value <= 0 || Map.Height == value) return;
            Map.Height = value;
            OnSizeChanged();
        }
    }

    public MapOption[] ParentOptions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public MapOption? SelectedParent
    {
        get => ParentOptions.FirstOrDefault(x => x.Guid == Map?.ParentGuid);
        set
        {
            if (value is null || Map is null || Map.ParentGuid == value.Guid) return;
            Map.ParentGuid = value.Guid;
            editor.OnParentChanged(value.Guid);
        }
    }

    public MapOption[] ConnectionOptions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public MapOption? NorthMap
    {
        get => FindConnection(Map?.North);
        set => SetConnection(value, (map, guid) => map.North = guid);
    }

    public MapOption? EastMap
    {
        get => FindConnection(Map?.East);
        set => SetConnection(value, (map, guid) => map.East = guid);
    }

    public MapOption? SouthMap
    {
        get => FindConnection(Map?.South);
        set => SetConnection(value, (map, guid) => map.South = guid);
    }

    public MapOption? WestMap
    {
        get => FindConnection(Map?.West);
        set => SetConnection(value, (map, guid) => map.West = guid);
    }

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

    public string[] BgmFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [AudioFiles.None];

    public string[] BgsFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [AudioFiles.None];

    public string? SelectedBgm
    {
        get => AudioFiles.NameOf(Map?.Bgm);
        set
        {
            if (value is null || Map is null) return;
            Map.Bgm = AudioFiles.FromName(Map.Bgm, value, "BGM");
            PlayBgmCommand.RaiseCanExecuteChanged();
        }
    }

    public string? SelectedBgs
    {
        get => AudioFiles.NameOf(Map?.Bgs);
        set
        {
            if (value is null || Map is null) return;
            Map.Bgs = AudioFiles.FromName(Map.Bgs, value, "BGS");
            PlayBgsCommand.RaiseCanExecuteChanged();
        }
    }

    #endregion

    #region Commands

    public AsyncRelayCommand FitToGroundCommand => field ??= new AsyncRelayCommand(FitToGround);

    public RelayCommand PlayBgmCommand => field ??= new RelayCommand(PlayBgm, () => Map?.Bgm is not null);

    public RelayCommand PlayBgsCommand => field ??= new RelayCommand(PlayBgs, () => Map?.Bgs is not null);

    public RelayCommand StopAudioCommand => field ??= new RelayCommand(StopAudio);

    #endregion

    #region Refresh

    // rebuilt every visit so newly added media show up
    public void RefreshMediaFiles()
    {
        BgmFiles = [AudioFiles.None, .. mediaService.GetMediaFiles(Cache.Media)];
        BgsFiles = [AudioFiles.None, .. AudioFiles.InFolder(mediaService.GetBgsPath())];
    }

    // everything shown for a newly selected map, or after its id changed
    public void Refresh()
    {
        SelectedMapId = Map is { } selected ? new MapIdModel(selected, gameDataService.GameMaps, OnMapIdChanged) : null;
        RefreshOptions();
        OnPropertyChanged(nameof(MapWidth));
        OnPropertyChanged(nameof(MapHeight));
        RefreshLayers();
        OnPropertyChanged(nameof(SelectedBgm));
        OnPropertyChanged(nameof(SelectedBgs));
        PlayBgmCommand.RaiseCanExecuteChanged();
        PlayBgsCommand.RaiseCanExecuteChanged();
    }

    // the parent and connection pickers: a map can't go inside itself or one of its own folders
    public void RefreshOptions()
    {
        var maps = gameDataService.GameMaps.OrderBy(x => x.Id).ToArray();
        var excluded = Map is null ? [] : MapTreeBuilder.Descendants(maps, Map.Guid).Append(Map.Guid).ToHashSet();

        ParentOptions = [new MapOption(null, "(Root)"), .. maps.Where(x => !excluded.Contains(x.Guid)).Select(ToOption)];
        ConnectionOptions = [new MapOption(null, AudioFiles.None), .. maps.Where(x => !x.FolderOnly && x.Guid != Map?.Guid).Select(ToOption)];

        OnPropertyChanged(nameof(SelectedParent));
        OnPropertyChanged(nameof(NorthMap));
        OnPropertyChanged(nameof(EastMap));
        OnPropertyChanged(nameof(SouthMap));
        OnPropertyChanged(nameof(WestMap));
    }

    public void StopAudio()
    {
        audioService.StopBgm();
        audioService.StopBgs();
    }

    #endregion

    #region Private Methods

    // the images can be added or removed outside the editor so they are looked up again every time
    private void RefreshLayers()
    {
        foreach (var layer in Layers) layer.PropertyChanged -= OnLayerChanged;
        foreach (var environment in Environments) environment.PropertyChanged -= OnLayerChanged;

        var graphicsDirectory = locationService.GraphicsDirectory!;
        if (Map is { FolderOnly: false } map)
        {
            Layers = [.. Enum.GetValues<MapLayerType>().Select(type => new MapLayerModel(map, type, graphicsDirectory)).OrderBy(x => x.Z)];
            Environments = [.. MapEnvironment.Names(graphicsDirectory).Select(name => new MapEnvironmentModel(map, name, graphicsDirectory))];
        }
        else
        {
            Layers = [];
            Environments = [];
        }

        foreach (var layer in Layers) layer.PropertyChanged += OnLayerChanged;
        foreach (var environment in Environments) environment.PropertyChanged += OnLayerChanged;
        LoadEnvironmentSizes(Environments).Forget();
    }

    // the frames are tiled over the map, so their size is needed before they can be drawn
    private async Task LoadEnvironmentSizes(MapEnvironmentModel[] environments)
    {
        foreach (var environment in environments.Where(x => x.FramePaths.Length > 0))
        {
            var (width, height) = await graphicsService.GetImageDimensions(environment.FramePaths[0]);
            environment.FrameSize = (width, height);
        }

        if (environments == Environments) editor.RebuildScene();
    }

    private void OnLayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        // raised once for every change to an image's z, opacity or blend, and when an environment is turned on or off
        if (e.PropertyName is nameof(MapImageModel.IsDefault) or nameof(MapEnvironmentModel.IsOn)) editor.RebuildScene();
    }

    private void OnSizeChanged()
    {
        OnPropertyChanged(nameof(MapWidth));
        OnPropertyChanged(nameof(MapHeight));
        editor.OnMapSizeChanged();
    }

    private async Task FitToGround()
    {
        if (Map is null || Layers.FirstOrDefault(x => x.Layer.Type == MapLayerType.Ground) is not { HasImage: true } ground) return;

        var (width, height) = await graphicsService.GetImageDimensions(ground.ImagePath);
        Map.Width = Math.Max(1, (int) Math.Ceiling(width / editor.TileSize));
        Map.Height = Math.Max(1, (int) Math.Ceiling(height / editor.TileSize));
        OnSizeChanged();
    }

    private async Task OnMapIdChanged(MapIdModel changed, int previousId)
    {
        var map = changed.Map;
        if (!await mapEditService.ResolveIdConflict(map)) map.Id = previousId;

        // the tree and the map options are sorted and labeled by id, and the images are found by it
        editor.OnMapIdChanged();
    }

    private void PlayBgm()
    {
        if (Map?.Bgm is not { Name: { } name } bgm || name.IsNullOrEmpty()) return;
        audioService.PlayBgm(name, bgm.Volume, bgm.Pitch).Forget();
    }

    private void PlayBgs()
    {
        if (Map?.Bgs is not { Name: { } name } bgs || name.IsNullOrEmpty()) return;
        audioService.PlayBgs(name, bgs.Volume, bgs.Pitch).Forget();
    }

    private MapOption? FindConnection(Guid? guid) => ConnectionOptions.FirstOrDefault(x => x.Guid == guid);

    private void SetConnection(MapOption? option, Action<GameMap, Guid?> apply)
    {
        // the combo box pushes null while its items are swapped
        if (option is null || Map is null) return;
        apply(Map, option.Guid);
    }

    private static MapOption ToOption(GameMap map) => new(map.Guid, $"{map.Id:000}: {map.Name}");

    #endregion
}
