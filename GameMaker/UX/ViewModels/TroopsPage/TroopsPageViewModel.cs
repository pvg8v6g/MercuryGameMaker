using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
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

    public string[] BackgroundFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string? SelectedBackground
    {
        get;
        set
        {
            if (value is null) return;
            SetField(ref field, value);
            _ = RefreshBackground();
        }
    } = None;

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
        get => SelectedEntity?.Bgm?.Volume ?? 1.0;
        set
        {
            if (SelectedEntity?.Bgm is null) return;
            SelectedEntity.Bgm.Volume = value;
            OnPropertyChanged();
        }
    }

    public double BgmPitch
    {
        get => SelectedEntity?.Bgm?.Pitch ?? 1.0;
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

    public RelayCommand PlayBgmCommand => field ??= new RelayCommand(PlayBgmAction, () => IsBgmSelected);

    public RelayCommand StopBgmCommand => field ??= new RelayCommand(audioService.StopBgm);

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        BgmFiles = [None, .. mediaService.GetMediaFiles(Cache.Media)];

        var groundPath = Path.Combine(locationService.GraphicsDirectory!, "Maps", "Ground");
        BackgroundFiles = Directory.Exists(groundPath)
            ? [None, .. Directory.GetFiles(groundPath, "*.png").Select(Path.GetFileName).OfType<string>()]
            : [None];

        // rebuilt every visit so enemies added on the enemies page show up
        EnemyNewOptions = Enemies
            .Select(x => new EngineListNewOption { Text = x.Name, Command = AddEnemyCommand, CommandParameter = x })
            .ToArray();

        RefreshMusic();
        return Task.CompletedTask;
    }

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        RefreshMusic();
        await RefreshTroopEnemies();
    }

    private void AddEnemyAction(Fighter enemy)
    {
        if (SelectedEntity is null) return;

        var troopEnemy = new TroopEnemy { EnemyGuid = enemy.Guid };
        SelectedEntity.Enemies.Add(troopEnemy);

        var model = new TroopEnemyModel(troopEnemy, enemy);
        TroopEnemies.Add(model);
        SelectedTroopEnemyIndex = TroopEnemies.Count - 1;
        _ = LoadSprite(model, enemy);
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
    }

    private void PlaceEnemyAction(Point point)
    {
        if (SelectedTroopEnemy is not { } model) return;
        model.X = (int) point.X;
        model.Y = (int) point.Y;
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
            .Select(x => (model: new TroopEnemyModel(x.troopEnemy, x.enemy), x.enemy))
            .ToArray();

        foreach (var (model, _) in models) TroopEnemies.Add(model);
        await Task.WhenAll(models.Select(x => LoadSprite(x.model, x.enemy)));
    }

    private async Task LoadSprite(TroopEnemyModel model, Fighter? enemy)
    {
        if (enemy is null || enemy.CharacterName.IsNullOrEmpty()) return;
        model.Sprite = await graphicsService.GetCharacter(enemy.CharacterName!, enemy.CharacterIndex, enemy.CharacterDirection);
    }

    // syncs the combo box to the selected troop without writing back to the troop
    private void RefreshMusic()
    {
        var name = SelectedEntity?.Bgm?.Name;
        _refreshingMusic = true;
        SelectedBgm = name.IsNullOrEmpty() ? None : name;
        _refreshingMusic = false;
    }

    private async Task RefreshBackground()
    {
        if (SelectedBackground == None)
        {
            BackgroundImage = null;
            return;
        }

        var path = Path.Combine(locationService.GraphicsDirectory!, "Maps", "Ground", SelectedBackground!);
        BackgroundImage = await graphicsService.GetImage(path);
    }

    #endregion
}
