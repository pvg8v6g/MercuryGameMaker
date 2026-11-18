using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using GameLibrary.Models;
using GameLibrary.Models.GameData;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameEngine.Services.Start;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace GameEngine.Services.Window;

/// <summary>
/// Sets up the game window: loads the game, goes full screen (or a centered, resizable window with --windowed) and works out
/// the screen, scale and game size. The game is the screen height tall and six tiles narrower than the screen, centered,
/// in a window the screen is the inside of the window and the game is sized again whenever the window is.
/// </summary>
public partial class WindowService(
    IGameDataService gameDataService,
    ILocationService locationService,
    IJsonService jsonService,
    IStartService startService) : IWindowService
{
    #region Events

    public event EventHandler? GameResized;

    #endregion

    #region Public Methods

    public async Task BuildGameEngine(Microsoft.UI.Xaml.Window window)
    {
        await locationService.CreateGameDirectory();
        LoadGame();

        // the window is sized first, a window's inside is smaller than the screen
        InitializeWindow(window);
        SetupScreenResolution(window);
        SetupScaleFactor();
        SetupGameResolution();

        // a window can be resized by dragging its edges, the game fits the new inside
        if (startService.IsWindowed) window.AppWindow.Changed += (_, args) => Resized(window, args);
    }

    #endregion

    #region Private Methods

    // what the map scene needs: the maps, the props placed on them, the actors the party leader comes from and the game start
    private void LoadGame()
    {
        Load(Path.Combine(locationService.GameDirectory!, "Maps"), gameDataService.GameMaps);
        Load(Path.Combine(locationService.GameDirectory!, "Fighters", "Actors"), gameDataService.Actors);
        Load(Path.Combine(locationService.GameDirectory!, "Props"), gameDataService.Props);

        var gameStart = Path.Combine(locationService.GameDirectory!, "Settings", "GameStart.data");
        if (File.Exists(gameStart) && jsonService.DecryptFile<GameStart>(gameStart) is { } start) gameDataService.GameStart = start;
    }

    private void Load<T>(string directory, ObservableCollection<T> collection) where T : BaseModel
    {
        collection.Clear();
        if (!Directory.Exists(directory)) return;

        foreach (var file in Directory.GetFiles(directory, "*.data"))
        {
            if (jsonService.DecryptData<T>(file) is { } item) collection.Add(item);
        }
    }

    // the screen (or the inside of the window) in view pixels, windows report physical pixels
    private void SetupScreenResolution(Microsoft.UI.Xaml.Window window)
    {
        var display = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = GetDpiForWindow(WindowNative.GetWindowHandle(window)) / 96.0;
        var (width, height) = startService.IsWindowed
            ? Inside(window.AppWindow.ClientSize, display.WorkArea)
            : (display.OuterBounds.Width, display.OuterBounds.Height);
        gameDataService.ScreenWidth = width / scale;
        gameDataService.ScreenHeight = height / scale;
    }

    // the window's inside, or the screen minus the taskbar if the window hasn't been sized yet
    private static (int Width, int Height) Inside(Windows.Graphics.SizeInt32 client, Windows.Graphics.RectInt32 workArea)
    {
        return client is { Width: > 0, Height: > 0 } ? (client.Width, client.Height) : (workArea.Width, workArea.Height);
    }

    private void SetupScaleFactor()
    {
        gameDataService.GridSize = 48;
        gameDataService.ScaleFactor = 2.0;
    }

    // never narrower than a tile, a window dragged smaller than the six tile border would give a negative width
    private void SetupGameResolution()
    {
        gameDataService.GameWidth = Math.Max(gameDataService.GridSize, gameDataService.ScreenWidth - (gameDataService.GridSize * 6.0));
        gameDataService.GameHeight = Math.Max(gameDataService.GridSize, gameDataService.ScreenHeight);
    }

    // a minimized window has no inside, the game keeps its size until the window comes back
    private void Resized(Microsoft.UI.Xaml.Window window, AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange || window.AppWindow.ClientSize is not { Width: > 0, Height: > 0 }) return;

        SetupScreenResolution(window);
        SetupGameResolution();
        GameResized?.Invoke(this, EventArgs.Empty);
    }

    // windowed keeps windows drawing the game like any other window, full screen can hand the game straight to the
    // display, which tears on some graphics drivers
    private void InitializeWindow(Microsoft.UI.Xaml.Window window)
    {
        if (!startService.IsWindowed)
        {
            window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            return;
        }

        window.AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            // dragging an edge or the maximize button resizes the game with it
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
        }

        // opens as a normal window, most of the screen and centered on it
        var workArea = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = (int)(workArea.Width * WindowedSize);
        var height = (int)(workArea.Height * WindowedSize);
        window.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            workArea.X + ((workArea.Width - width) / 2),
            workArea.Y + ((workArea.Height - height) / 2),
            width,
            height));
    }

    // how much of the screen a window opens at
    private const double WindowedSize = 0.8;

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);

    #endregion
}
