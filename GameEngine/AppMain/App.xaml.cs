using GameLibrary.Utilities;
using GameEngine.Navigation;
using GameEngine.Pages;
using GameEngine.Pages.Handlers;
using GameEngine.Scenes;
using GameEngine.Services.Conditions;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Services.Party;
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
        services.AddSingleton<PartyService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<PathWalker>();

        #endregion

        #region Register Systems

        // systems run in this order every update
        AddSystem<ActivePageSystem>(services);
        AddSystem<InputSystem>(services);
        AddSystem<MovementRouteSystem>(services);
        AddSystem<PatrolSystem>(services);
        AddSystem<MovementSystem>(services);
        AddSystem<HitboxSystem>(services);
        AddSystem<CharacterImageSystem>(services);
        AddSystem<AnimationSystem>(services);
        AddSystem<BalloonSystem>(services);

        #endregion

        #region Register Pages

        services.AddSingleton<PageTouches>();
        services.AddSingleton<ActionRunner>();
        services.AddSingleton<PageRunner>();

        #endregion

        #region Register Action Handlers

        // each page action is run by the handler for its type, an action with none is skipped
        services.AddSingleton<IActionHandler, FlowHandler>();
        services.AddSingleton<IActionHandler, BranchHandler>();
        services.AddSingleton<IActionHandler, WaitHandler>();
        services.AddSingleton<IActionHandler, ExitProcessingHandler>();
        services.AddSingleton<IActionHandler, EntityEraseHandler>();
        services.AddSingleton<IActionHandler, EntityMovementHandler>();
        services.AddSingleton<IActionHandler, EntityBalloonIconHandler>();
        services.AddSingleton<IActionHandler, EntityOpacityHandler>();
        services.AddSingleton<IActionHandler, EntityAnimationHandler>();
        services.AddSingleton<IActionHandler, EntityTintHandler>();
        services.AddSingleton<IActionHandler, WeatherHandler>();
        services.AddSingleton<IActionHandler, EntityTransferHandler>();

        // party and actors
        services.AddSingleton<IActionHandler, MoneyHandler>();
        services.AddSingleton<IActionHandler, ChangeItemHandler>();
        services.AddSingleton<IActionHandler, PartyHandler>();
        services.AddSingleton<IActionHandler, RecoverHandler>();
        services.AddSingleton<IActionHandler, ActorNameHandler>();
        services.AddSingleton<IActionHandler, ActorDescriptionHandler>();
        services.AddSingleton<IActionHandler, ActorImageHandler>();
        services.AddSingleton<IActionHandler, ActorAttributeHandler>();
        services.AddSingleton<IActionHandler, ActorLifeHandler>();
        services.AddSingleton<IActionHandler, ActorManaHandler>();
        services.AddSingleton<IActionHandler, ActorExperienceHandler>();
        services.AddSingleton<IActionHandler, ActorLevelHandler>();
        services.AddSingleton<IActionHandler, ActorArteHandler>();
        services.AddSingleton<IActionHandler, ActorStateHandler>();
        services.AddSingleton<IActionHandler, ActorEquipmentHandler>();

        // screen and media
        services.AddSingleton<IActionHandler, ScreenFadeHandler>();
        services.AddSingleton<IActionHandler, ScrollMapHandler>();
        services.AddSingleton<IActionHandler, PlayMediaHandler>();
        services.AddSingleton<IActionHandler, FadeMediaHandler>();

        #endregion

        #region Register Scenes

        services.AddSingleton<Renderer>();
        services.AddSingleton<GameCamera>();
        services.AddSingleton<ScreenFade>();
        services.AddSingleton<PlayerSpawner>();
        services.AddSingleton<MapAudio>();
        services.AddSingleton<MapWeather>();
        services.AddSingleton<DebugOverlay>();
        services.AddSingleton<MapScene>();

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        InitializeComponent();

        // a crash closes the window with no message, this leaves the reason next to the exe
        UnhandledException += (_, e) => WriteCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrash(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => WriteCrash(e.Exception);
        ForgetExtensions.Failed += WriteCrash;
    }

    private static void WriteCrash(Exception? exception)
    {
        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), $"{DateTime.Now}\n{exception}\n\n");
        }
        catch (IOException)
        {
        }
    }

    // the game is loaded and sized before the window shows, the game view starts on the first map once it loads
    // WinUI's override has to return void, so the real work is a Task that can't take the process down unseen
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = Launch();
    }

    private async Task Launch()
    {
        try
        {
            _window = Services!.GetRequiredService<MainWindow>();
            await Services!.GetRequiredService<IWindowService>().BuildGameEngine(_window);
            _window.Activate();
        }
        catch (Exception exception)
        {
            // the game can't start without its window, leave the reason in crash.log and close
            WriteCrash(exception);
            Exit();
        }
    }

    private Window? _window;

    // a system is injected by type into the systems that use it and listed with every other system for the scene
    private static void AddSystem<T>(ServiceCollection services) where T : class, IEntitySystem
    {
        services.AddSingleton<T>();
        services.AddSingleton<IEntitySystem>(provider => provider.GetRequiredService<T>());
    }
}
