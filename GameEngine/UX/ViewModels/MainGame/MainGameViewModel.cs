using System.Collections.ObjectModel;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Start;
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
    IStartService startService) : BaseViewModel
{
    #region Properties

    // its Nodes are swapped for a new list whenever the picture changes
    public Renderer Renderer => mapScene.Renderer;

    public DebugOverlay DebugOverlay => mapScene.DebugOverlay;

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
        OnPropertyChanged(nameof(GameWidth));
        OnPropertyChanged(nameof(GameHeight));
        OnPropertyChanged(nameof(GameBounds));
        if (startService.Find() is not { } start) return;

        // a new game, the party is the game start's party
        gameStateService.ActiveParty.Clear();
        gameStateService.ActiveParty.UnionWith(gameDataService.GameStart.Party);
        gameStateService.OwnedParty.UnionWith(gameDataService.GameStart.Party);

        await mapScene.Load(start.Map, start.Spawn);
    }

    protected override void Update()
    {
        mapScene.Update();
    }

    #endregion
}
