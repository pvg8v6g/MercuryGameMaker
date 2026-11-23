using System.Collections.ObjectModel;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;
using GameEngine.Interface;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Start;
using GameEngine.Services.Window;
using GameLibrary.Commands;
using GameLibrary.Services.GameData;
using MercuryLibrary.ImageComponents;

namespace GameEngine.UX.ViewModels.MainGame;

/// <summary>
/// The game being played on the map.
/// </summary>
public partial class MainGameViewModel(
    MapScene mapScene,
    IInputService inputService,
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IStartService startService,
    IWindowService windowService,
    BattleHud battleHud) : BaseViewModel
{
    #region Properties

    // its Nodes are swapped for a new list whenever the picture changes
    public Renderer Renderer => mapScene.Renderer;

    public DebugOverlay DebugOverlay => mapScene.DebugOverlay;

    public BattleHud BattleHud => battleHud;

    public double GameWidth => gameDataService.GameWidth;

    public double GameHeight => gameDataService.GameHeight;

    public Rect GameBounds => new(0, 0, GameWidth, GameHeight);

    public double FrameRate => gameDataService.FrameRate;

    #endregion

    #region Commands

    public RelayCommand<VirtualKey> KeyDownCommand => field ??= new RelayCommand<VirtualKey>(inputService.KeyPressed);

    public RelayCommand<VirtualKey> KeyUpCommand => field ??= new RelayCommand<VirtualKey>(inputService.KeyReleased);

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        // the page can load again, it only listens once
        windowService.GameResized -= GameResized;
        windowService.GameResized += GameResized;
        RefreshGameSize();
        if (startService.Find() is not { } start) return;

        // a new game, the party is the game start's party
        gameStateService.ActiveParty.Clear();
        gameStateService.ActiveParty.AddRange(gameDataService.GameStart.Party.Distinct());
        gameStateService.OwnedParty.UnionWith(gameDataService.GameStart.Party);

        await mapScene.Load(start.Map, start.Spawn);
    }

    protected override void Update()
    {
        mapScene.Update();
    }

    #endregion

    #region Methods

    private void GameResized(object? sender, EventArgs e)
    {
        RefreshGameSize();
    }

    private void RefreshGameSize()
    {
        OnPropertyChanged(nameof(GameWidth));
        OnPropertyChanged(nameof(GameHeight));
        OnPropertyChanged(nameof(GameBounds));
    }

    #endregion
}
