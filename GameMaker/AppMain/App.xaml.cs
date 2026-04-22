using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Tasks;
using GameMaker.Services.Navigation;
using GameMaker.Tasks;
using GameMaker.UX.ViewModels.ActorsPage;
using GameMaker.UX.ViewModels.AnimationsPage;
using GameMaker.UX.ViewModels.AttributesPage;
using GameMaker.UX.ViewModels.DisciplinesPage;
using GameMaker.UX.ViewModels.ElementsPage;
using GameMaker.UX.ViewModels.EquipmentPage;
using GameMaker.UX.ViewModels.GrowthsPage;
using GameMaker.UX.ViewModels.HomeView;
using GameMaker.UX.ViewModels.StatesPage;
using GameMaker.UX.ViewModels.TopBar;
using GameMaker.UX.Views.ActorsPage;
using GameMaker.UX.Views.AnimationsPage;
using GameMaker.UX.Views.AttributesPage;
using GameMaker.UX.Views.DisciplinesPage;
using GameMaker.UX.Views.ElementsPage;
using GameMaker.UX.Views.EquipmentPage;
using GameMaker.UX.Views.GrowthsPage;
using GameMaker.UX.Views.HomeView;
using GameMaker.UX.Views.MainWindow;
using GameMaker.UX.Views.StatesPage;
using GameMaker.UX.Views.TopBar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace GameMaker.AppMain;

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

        services.AddSingleton<TopBarViewModel>();
        services.AddSingleton<HomeViewViewModel>();
        services.AddSingleton<ActorsPageViewModel>();
        services.AddSingleton<AttributesPageViewModel>();
        services.AddSingleton<ElementsPageViewModel>();
        services.AddSingleton<GrowthsPageViewModel>();
        services.AddSingleton<DisciplinesPageViewModel>();
        services.AddSingleton<EquipmentPageViewModel>();
        services.AddSingleton<StatesPageViewModel>();
        services.AddSingleton<AnimationsPageViewModel>();

        #endregion

        #region Register Views

        services.AddSingleton<MainWindow>();
        services.AddSingleton<HomeView>();
        services.AddSingleton<TopBar>();
        services.AddSingleton<ActorsPage>();
        services.AddSingleton<AttributesPage>();
        services.AddSingleton<ElementsPage>();
        services.AddSingleton<GrowthsPage>();
        services.AddSingleton<DisciplinesPage>();
        services.AddSingleton<EquipmentPage>();
        services.AddSingleton<StatesPage>();
        services.AddSingleton<AnimationsPage>();

        #endregion

        #region Register Tasks

        services.AddSingleton<LoadDataTask>();
        services.AddSingleton<SaveDataTask>();

        #endregion

        #region Register Services

        services.AddSingleton<Func<Type, EngineTask>>(provider => taskType => (EngineTask) provider.GetRequiredService(taskType));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILocationService, LocationService>();
        services.AddSingleton<IGraphicsService, GraphicsService>();
        services.AddSingleton<IGameDataService, GameDataService>();
        services.AddSingleton<IJsonService, JsonService>();

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services?.GetRequiredService<MainWindow>();
        _window?.Activate();
    }

    private Window? _window;
}
