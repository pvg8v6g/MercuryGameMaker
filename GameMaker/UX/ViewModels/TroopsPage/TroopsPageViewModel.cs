using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Media;
using GameLibrary.Models.Troops;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Services.Media;
using GameMaker.UX.Components.EngineListView;
using GameMaker.UX.Models.TroopsPage;
using MercuryLibrary.Extensions;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.ViewModels.TroopsPage;

public partial class TroopsPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    IMediaService mediaService,
    IAudioService audioService) : BaseViewModel<Troop>(jsonService)
{
    #region Constants

    private const string None = "(None)";

    // the most fighters the party can bring into a battle, so a troop wants this many party spawn points
    private const int PartyBattlers = 4;

    // what a left-click in the arena does
    private const string PlaceEnemy = "Selected Enemy";
    private const string PlaceEnemyPoints = "Enemy Spawn Points";
    private const string PlacePartyPoints = "Party Spawn Points";

    #endregion

    #region Fields

    private bool _refreshingMusic;

    #endregion

    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Troop> EntityCollection => gameDataService.Troops;

    public ObservableCollection<Fighter> Enemies => gameDataService.Enemies;

    public ObservableCollection<TroopEnemyModel> TroopEnemies { get; } = [];

    // the "Add" flyout of the enemies list - one entry per enemy from the enemies page
    public IReadOnlyList<EngineListNewOption> EnemyNewOptions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public int SelectedTroopEnemyIndex
    {
        get;
        set
        {
            SetField(ref field, value);
            for (var i = 0; i < TroopEnemies.Count; i++) TroopEnemies[i].IsSelected = i == value;
            OnPropertyChanged(nameof(SelectedTroopEnemy));
            OnPropertyChanged(nameof(HasSelectedTroopEnemy));
            OnPropertyChanged(nameof(SelectedEnemySpawn));
        }
    } = -1;

    public TroopEnemyModel? CopiedTroopEnemy
    {
        get;
        private set => SetField(ref field, value);
    }

    public TroopEnemyModel? SelectedTroopEnemy => SelectedTroopEnemyIndex >= 0 && SelectedTroopEnemyIndex < TroopEnemies.Count
        ? TroopEnemies[SelectedTroopEnemyIndex]
        : null;

    public bool HasSelectedTroopEnemy => SelectedTroopEnemy is not null;

    // the selected enemy starts on its own spot (Fixed) or on one of the troop's enemy spawn points (Random)
    public SpawnMode SelectedEnemySpawn
    {
        get => SelectedTroopEnemy?.Spawn ?? SpawnMode.Fixed;
        set
        {
            if (SelectedTroopEnemy is not { } model || model.Spawn == value) return;
            model.Spawn = value;
            OnPropertyChanged();
            RefreshWarning();
        }
    }

    // the party starts on its points in party order (Fixed) or on points picked at random (Random)
    public SpawnMode PartySpawn
    {
        get => SelectedEntity?.PartySpawn ?? SpawnMode.Fixed;
        set
        {
            if (SelectedEntity is null || SelectedEntity.PartySpawn == value) return;
            SelectedEntity.PartySpawn = value;
            OnPropertyChanged();
        }
    }

    // the party and enemy spawn points drawn in the arena
    public ObservableCollection<SpawnPointModel> SpawnPoints { get; } = [];

    public string[] PlaceModes { get; } = [PlaceEnemy, PlaceEnemyPoints, PlacePartyPoints];

    public string SelectedPlaceMode
    {
        get;
        set
        {
            if (value is null) return;
            SetField(ref field, value);
        }
    } = PlaceEnemy;

    // what's missing for this troop to start a battle the way it's set up, empty when nothing is
    public string SpawnWarning
    {
        get
        {
            if (SelectedEntity is not { } troop) return string.Empty;

            var warnings = new List<string>();
            if (troop.PartySpawnPoints.Count < PartyBattlers)
                warnings.Add($"The party can bring {PartyBattlers} fighters, but there are only {troop.PartySpawnPoints.Count} party spawn points.");

            var random = troop.Enemies.Count(x => x.Spawn == SpawnMode.Random);
            if (random > troop.EnemySpawnPoints.Count)
                warnings.Add($"{random} enemies spawn at random, but there are only {troop.EnemySpawnPoints.Count} enemy spawn points.");

            var unplaced = troop.Enemies.Count(x => x.Spawn == SpawnMode.Fixed && (x.X is null || x.Y is null));
            if (unplaced > 0) warnings.Add($"{unplaced} fixed {(unplaced == 1 ? "enemy hasn't" : "enemies haven't")} been placed.");

            return string.Join(Environment.NewLine, warnings);
        }
    }

    public bool HasSpawnWarning => SpawnWarning.Length > 0;

    #endregion

    #region Arena Properties

    public int ArenaTiles
    {
        get;
        set
        {
            if (value <= 0) return;
            SetField(ref field, value);
            OnPropertyChanged(nameof(ArenaSize));
        }
    } = 21;

    public int ArenaSize => ArenaTiles * gameDataService.GridSize;

    public int BlockSize
    {
        get;
        set
        {
            if (value <= 0) return;
            SetField(ref field, value);
        }
    } = 48;

    public bool ShowGrid
    {
        get;
        set => SetField(ref field, value);
    } = true;

    // the maps a troop can be fought on: every map with Battle Map ticked
    public GameMap[] BattleMaps
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public GameMap? SelectedBattleMap
    {
        get => SelectedEntity?.BattleMapId is { } id ? BattleMaps.FirstOrDefault(x => x.Guid == id) : null;
        set
        {
            // the combo box pushes null while its items are swapped, that never clears the troop's map
            if (value is null || SelectedEntity is null || SelectedEntity.BattleMapId == value.Guid) return;
            SelectedEntity.BattleMapId = value.Guid;
            OnPropertyChanged();
            _ = RefreshBackground();
        }
    }

    public BitmapImage? BackgroundImage
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Music Properties

    public string[] BgmFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string? SelectedBgm
    {
        get;
        set
        {
            // the combo box pushes null while its items are swapped - ignore it so the troop keeps its music
            if (value is null) return;
            SetField(ref field, value);

            if (SelectedEntity is not null && !_refreshingMusic)
            {
                var normalized = value.IsNullOrEmpty() ? None : value;
                if (normalized == None)
                {
                    SelectedEntity.Bgm = null;
                }
                else
                {
                    SelectedEntity.Bgm ??= new AudioFile { Path = "BGM" };
                    SelectedEntity.Bgm.Name = normalized;
                }
            }

            OnPropertyChanged(nameof(IsBgmSelected));
            OnPropertyChanged(nameof(BgmVolume));
            OnPropertyChanged(nameof(BgmPitch));
            PlayBgmCommand.RaiseCanExecuteChanged();
        }
    } = None;

    public bool IsBgmSelected => SelectedEntity?.Bgm is not null;

    public double BgmVolume
    {
        get => SelectedEntity?.Bgm?.Volume ?? AudioDefaults.Volume;
        set
        {
            if (SelectedEntity?.Bgm is null) return;
            SelectedEntity.Bgm.Volume = value;
            OnPropertyChanged();
        }
    }

    public double BgmPitch
    {
        get => SelectedEntity?.Bgm?.Pitch ?? AudioDefaults.Pitch;
        set
        {
            if (SelectedEntity?.Bgm is null) return;
            SelectedEntity.Bgm.Pitch = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    public RelayCommand<Fighter> AddEnemyCommand => field ??= new RelayCommand<Fighter>(AddEnemyAction);

    public RelayCommand CopyEnemyCommand => field ??= new RelayCommand(CopyEnemyAction);

    public RelayCommand PasteEnemyCommand => field ??= new RelayCommand(PasteEnemyAction);

    public RelayCommand DeleteEnemyCommand => field ??= new RelayCommand(DeleteEnemyAction);

    public RelayCommand<Point> PlaceEnemyCommand => field ??= new RelayCommand<Point>(PlaceEnemyAction);

    public RelayCommand<TroopEnemyModel> SelectArenaEnemyCommand => field ??= new RelayCommand<TroopEnemyModel>(SelectArenaEnemyAction);

    public RelayCommand<SpawnPointModel> RemoveSpawnPointCommand => field ??= new RelayCommand<SpawnPointModel>(RemoveSpawnPointAction);

    public RelayCommand PlayBgmCommand => field ??= new RelayCommand(PlayBgmAction, () => IsBgmSelected);

    public RelayCommand StopBgmCommand => field ??= new RelayCommand(audioService.StopBgm);

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        BgmFiles = [None, .. mediaService.GetMediaFiles(Cache.Media)];

        BattleMaps = [.. gameDataService.GameMaps.Where(x => x.BattleMap).OrderBy(x => x.Name)];
        OnPropertyChanged(nameof(SelectedBattleMap));
        _ = RefreshBackground();

        // rebuilt every visit so enemies added on the enemies page show up
        EnemyNewOptions =
        [
            .. Enemies
                .Select(x => new EngineListNewOption { Text = x.Name, Command = AddEnemyCommand, CommandParameter = x })
        ];

        RefreshMusic();
        return Task.CompletedTask;
    }

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        RefreshMusic();
        OnPropertyChanged(nameof(PartySpawn));
        OnPropertyChanged(nameof(SelectedBattleMap));
        await RefreshBackground();
        RefreshSpawnPoints();
        await RefreshTroopEnemies();
        RefreshWarning();
    }

    private void AddEnemyAction(Fighter enemy)
    {
        if (SelectedEntity is null) return;

        var troopEnemy = new TroopEnemy { EnemyGuid = enemy.Guid };
        SelectedEntity.Enemies.Add(troopEnemy);

        var model = new TroopEnemyModel(troopEnemy, enemy, gameDataService.GridSize);
        TroopEnemies.Add(model);
        SelectedTroopEnemyIndex = TroopEnemies.Count - 1;
        _ = LoadSprite(model, enemy);
        RefreshWarning();
    }

    private void CopyEnemyAction()
    {
        CopiedTroopEnemy = SelectedTroopEnemy;
    }

    // pasted enemies start unplaced and selected so the next arena click places them
    private void PasteEnemyAction()
    {
        if (CopiedTroopEnemy is null) return;
        var enemy = Enemies.FirstOrDefault(x => x.Guid == CopiedTroopEnemy.TroopEnemy.EnemyGuid);
        if (enemy is not null) AddEnemyAction(enemy);
    }

    private void DeleteEnemyAction()
    {
        if (SelectedEntity is null || SelectedTroopEnemy is not { } model) return;

        var index = SelectedTroopEnemyIndex;
        SelectedEntity.Enemies.Remove(model.TroopEnemy);
        TroopEnemies.Remove(model);
        SelectedTroopEnemyIndex = Math.Min(index, TroopEnemies.Count - 1);
        RefreshWarning();
    }

    // the clicked tile is where the feet go: the selected enemy stands there, or a spawn point is added there
    private void PlaceEnemyAction(Point point)
    {
        if (SelectedEntity is not { } troop) return;

        var (x, y) = ((int) point.X, (int) point.Y);
        switch (SelectedPlaceMode)
        {
            case PlaceEnemyPoints:
                AddSpawnPoint(troop.EnemySpawnPoints, x, y);
                break;

            case PlacePartyPoints:
                AddSpawnPoint(troop.PartySpawnPoints, x, y);
                break;

            default:
                if (SelectedTroopEnemy is not { } model) return;

                // placing an enemy on a spot of its own makes it Fixed
                model.Spawn = SpawnMode.Fixed;
                model.X = x;
                model.Y = y;
                OnPropertyChanged(nameof(SelectedEnemySpawn));
                break;
        }

        RefreshWarning();
    }

    // one point per tile for each side
    private void AddSpawnPoint(ObservableCollection<TroopSpawnPoint> points, int x, int y)
    {
        if (points.Any(p => p.X == x && p.Y == y)) return;

        points.Add(new TroopSpawnPoint { X = x, Y = y });
        RefreshSpawnPoints();
    }

    private void RemoveSpawnPointAction(SpawnPointModel model)
    {
        if (SelectedEntity is not { } troop) return;

        if (model.IsParty) troop.PartySpawnPoints.Remove(model.Point);
        else troop.EnemySpawnPoints.Remove(model.Point);
        RefreshSpawnPoints();
        RefreshWarning();
    }

    private void SelectArenaEnemyAction(TroopEnemyModel model)
    {
        SelectedTroopEnemyIndex = TroopEnemies.IndexOf(model);
    }

    private void PlayBgmAction()
    {
        if (SelectedEntity?.Bgm?.Name is not { } name || name.IsNullOrEmpty()) return;
        audioService.PlayBgm(name, SelectedEntity.Bgm.Volume, SelectedEntity.Bgm.Pitch);
    }

    #endregion

    #region Private Methods

    private async Task RefreshTroopEnemies()
    {
        SelectedTroopEnemyIndex = -1;
        TroopEnemies.Clear();
        if (SelectedEntity is null) return;

        var models = SelectedEntity.Enemies
            .Select(x => (troopEnemy: x, enemy: Enemies.FirstOrDefault(e => e.Guid == x.EnemyGuid)))
            .Select(x => (model: new TroopEnemyModel(x.troopEnemy, x.enemy, gameDataService.GridSize), x.enemy))
            .ToArray();

        foreach (var (model, _) in models) TroopEnemies.Add(model);
        await Task.WhenAll(models.Select(x => LoadSprite(x.model, x.enemy)));
    }

    private async Task LoadSprite(TroopEnemyModel model, Fighter? enemy)
    {
        if (enemy is null || enemy.CharacterName.IsNullOrEmpty()) return;
        model.Sprite = await graphicsService.GetCharacter(enemy.CharacterName!, enemy.CharacterIndex, enemy.CharacterDirection);
    }

    // party points are numbered in party order, enemy points are all alike
    private void RefreshSpawnPoints()
    {
        SpawnPoints.Clear();
        if (SelectedEntity is not { } troop) return;

        for (var i = 0; i < troop.PartySpawnPoints.Count; i++) SpawnPoints.Add(new SpawnPointModel(troop.PartySpawnPoints[i], true, (i + 1).ToString()));
        foreach (var point in troop.EnemySpawnPoints) SpawnPoints.Add(new SpawnPointModel(point, false, string.Empty));
    }

    private void RefreshWarning()
    {
        OnPropertyChanged(nameof(SpawnWarning));
        OnPropertyChanged(nameof(HasSpawnWarning));
    }

    // syncs the combo box to the selected troop without writing back to the troop
    private void RefreshMusic()
    {
        var name = SelectedEntity?.Bgm?.Name;
        _refreshingMusic = true;
        SelectedBgm = name.IsNullOrEmpty() ? None : name;
        _refreshingMusic = false;
    }

    // the arena shows the troop's battle map: its ground picture, at the map's size
    private async Task RefreshBackground()
    {
        if (SelectedBattleMap is not { } map)
        {
            BackgroundImage = null;
            return;
        }

        ArenaTiles = Math.Max(map.Width, map.Height);
        var path = new MapLayer(MapLayerType.Ground).ImagePath(locationService.GraphicsDirectory!, map.Id);
        BackgroundImage = File.Exists(path) ? await graphicsService.GetImage(path) : null;
    }

    #endregion
}
