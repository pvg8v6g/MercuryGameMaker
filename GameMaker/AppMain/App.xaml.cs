using GameLibrary.Services.Location;
using GameMaker.Services.Navigation;
using GameMaker.Tasks;
using GameMaker.UX.ViewModels;
using GameMaker.UX.ViewModels.HomeView;
using GameMaker.UX.ViewModels.TopBar;
using GameMaker.UX.Views.HomeView;
using GameMaker.UX.Views.MainWindow;
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

        #endregion

        #region Register Views

        services.AddSingleton<MainWindow>();
        services.AddSingleton<HomeView>(provider => new HomeView { DataContext = provider.GetRequiredService<HomeViewViewModel>() });
        services.AddSingleton<TopBar>(provider => new TopBar { DataContext = provider.GetRequiredService<TopBarViewModel>() });

        #endregion

        #region Register Services

        services.AddSingleton<Func<Type, BaseViewModel>>(provider => viewModelType => (BaseViewModel) provider.GetRequiredService(viewModelType));
        services.AddSingleton<Func<Type, EngineTask>>(provider => taskType => (EngineTask) provider.GetRequiredService(taskType));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILocationService, LocationService>();

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services?.GetRequiredService<MainWindow>();

        var navigationService = Services?.GetRequiredService<INavigationService>();
        navigationService?.NavigateTo<HomeViewViewModel>();

        _window?.Activate();
    }

    private Window? _window;
}
