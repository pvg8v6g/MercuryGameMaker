using System.Runtime.InteropServices;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace GameEngine.Services.Window;

/// <summary>
/// Sets up the game window: loads the game, works out the screen, scale and game size and goes full screen.
/// The game is the screen height tall and six tiles narrower than the screen, centered.
/// </summary>
public partial class WindowService(IGameDataService gameDataService, ILocationService locationService, IJsonService jsonService) : IWindowService
{
    #region Public Methods

    public async Task BuildGameEngine(Microsoft.UI.Xaml.Window window)
    {
        await locationService.CreateGameDirectory();
        LoadMaps();

        SetupScreenResolution(window);
        SetupScaleFactor();
        SetupGameResolution();
        InitializeWindow(window);
    }

    #endregion

    #region Private Methods

    private void LoadMaps()
    {
        var directory = Path.Combine(locationService.GameDirectory!, "Maps");
        if (!Directory.Exists(directory)) return;

        gameDataService.GameMaps.Clear();
        foreach (var file in Directory.GetFiles(directory, "*.data"))
        {
            if (jsonService.DecryptData<GameMap>(file) is { } map) gameDataService.GameMaps.Add(map);
        }
    }

    // the screen in view pixels, the display reports physical pixels
    private void SetupScreenResolution(Microsoft.UI.Xaml.Window window)
    {
        var display = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = GetDpiForWindow(WindowNative.GetWindowHandle(window)) / 96.0;
        gameDataService.ScreenWidth = display.OuterBounds.Width / scale;
        gameDataService.ScreenHeight = display.OuterBounds.Height / scale;
    }

    private void SetupScaleFactor()
    {
        gameDataService.GridSize = 48;
        gameDataService.ScaleFactor = 2.0;
    }

    private void SetupGameResolution()
    {
        gameDataService.GameWidth = gameDataService.ScreenWidth - (gameDataService.GridSize * 6.0);
        gameDataService.GameHeight = gameDataService.ScreenHeight;
    }

    private static void InitializeWindow(Microsoft.UI.Xaml.Window window)
    {
        window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    #endregion
}
