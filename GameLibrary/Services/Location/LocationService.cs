using GameLibrary.Configuration;
using GameLibrary.Services.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GameLibrary.Services.Location;

public class LocationService(IJsonService jsonService) : ILocationService
{
    #region Properties

    public string? GameDirectory { get; private set; }

    public string? GraphicsDirectory { get; private set; }

    public string? GameMakerGraphicsDirectory { get; private set; }

    public string? MercuryGameMakerDirectory { get; private set; }

    #endregion

    #region Create Game Directory

    public void CreateMercuryGameDirectory()
    {
        MercuryGameMakerDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mercury Game Maker");
        if (!Directory.Exists(MercuryGameMakerDirectory)) Directory.CreateDirectory(MercuryGameMakerDirectory);
        var config = new MercuryGameMakerConfiguration();
        var configPath = Path.Combine(MercuryGameMakerDirectory, "config.json");
        jsonService.EncryptFile(config, configPath);
    }

    public async Task CreateGameDirectory()
    {
        if (MercuryGameMakerDirectory is null) CreateMercuryGameDirectory();
        var configPath = Path.Combine(MercuryGameMakerDirectory!, "config.json");
        var config = jsonService.DecryptFile<MercuryGameMakerConfiguration>(configPath);
        if (config is null)
        {
            ShowErrorMessageAndExit("Config Not Found", "The application will now exit.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.GameDirectory))
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.CommitButtonText = "Select Game Directory";

            var mainWindow = Type.GetType("GameMaker.UX.Views.MainWindow.MainWindow, GameMaker")?.GetProperty("Window")?.GetValue(null);
            if (mainWindow == null) throw new InvalidOperationException("MainWindow not found");

            var hwnd = WindowNative.GetWindowHandle(mainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                ShowErrorMessageAndExit("Game Directory Required", "A game directory must be chosen. The application will now exit.");
                return;
            }

            config.GameDirectory = folder.Path;
            jsonService.EncryptFile(config, configPath);
            GameDirectory = config.GameDirectory;
        }

        if (GameDirectory is null)
        {
            ShowErrorMessageAndExit("Game Directory Required", "A game directory must be chosen. The application will now exit.");
            return;
        }

        GraphicsDirectory = Path.Combine(GameDirectory, "Graphics");
        Directory.CreateDirectory(GameDirectory);
        Directory.CreateDirectory(GraphicsDirectory);
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Animations"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Artes"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Disciplines"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Fighters"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Fighters", "Actors")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Fighters", "Enemies")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Items"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Items", "Consumables")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Items", "Equipment")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Maps"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Media"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "BGM")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "BGS")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "Footsteps")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "ME")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "SFX")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Settings"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "States"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Troops"));

        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Animations"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Characters"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Enemies"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Faces"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Icons"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Maps"));
    }

    private void ShowErrorMessageAndExit(string title, string message)
    {
        // Using Win32 MessageBox for a reliable "exit" popup from a class library
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        var mainWindow = Type.GetType("GameMaker.UX.Views.MainWindow.MainWindow, GameMaker")?.GetProperty("Window")?.GetValue(null);
        var hwnd = mainWindow != null ? WindowNative.GetWindowHandle(mainWindow) : IntPtr.Zero;

        MessageBox(hwnd, message, title, 0x00000010); // MB_ICONERROR = 0x00000010

        Environment.Exit(0);
    }

    #endregion

    #region Set GameMaker Graphics Directory

    public void SetGameMakerGraphicsDirectory(string path)
    {
        GameMakerGraphicsDirectory = path;
    }

    #endregion
}
