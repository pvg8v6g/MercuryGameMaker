using GameEngine.Scenes;
using GameEngine.Services.Conditions;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Start;
using GameEngine.Services.Window;
using GameEngine.Systems;
using GameEngine.UX.ViewModels.MainGame;
using GameEngine.UX.Views.MainGame;
using GameEngine.UX.Views.MainWindow;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Services.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace GameEngine.AppMain;

public partial class App
{
    #region Fields

    private static ServiceProvider? ServiceProvider { get; set; }

    #endregion

    public static IServiceProvider? Services => ServiceProvider;

    public App()
    {
        var services = new ServiceCollection();

        #region Register View Models

        services.AddSingleton<MainGameViewModel>();

        #endregion

        #region Register Views

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainGameView>();

        #endregion

        #region Register Services

        services.AddSingleton<ILocationService, LocationService>();
        services.AddSingleton<IGraphicsService, GraphicsService>();
        services.AddSingleton<IGameDataService, GameDataService>();
        services.AddSingleton<IJsonService, JsonService>();
        services.AddSingleton<IMediaService, MediaService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<IConditionService, ConditionService>();
        services.AddSingleton<IStartService, StartService>();

        #endregion

        #region Register Systems

        // systems run in this order every update
        AddSystem<ActivePageSystem>(services);
        AddSystem<InputSystem>(services);
        AddSystem<MovementSystem>(services);
        AddSystem<HitboxSystem>(services);
        AddSystem<CharacterImageSystem>(services);

        #endregion

        #region Register Scenes

        services.AddSingleton<Renderer>();
        services.AddSingleton<PlayerSpawner>();
        services.AddSingleton<MapAudio>();
        services.AddSingleton<DebugOverlay>();
        services.AddSingleton<MapScene>();

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        InitializeComponent();
    }

    // the game is loaded and sized before the window shows, the game view starts on the first map once it loads
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services!.GetRequiredService<MainWindow>();
        await Services!.GetRequiredService<IWindowService>().BuildGameEngine(_window);
        _window.Activate();
    }

    private Window? _window;

    // a system is injected by type into the systems that use it and listed with every other system for the scene
    private static void AddSystem<T>(ServiceCollection services) where T : class, IEntitySystem
    {
        services.AddSingleton<T>();
        services.AddSingleton<IEntitySystem>(provider => provider.GetRequiredService<T>());
    }
}
