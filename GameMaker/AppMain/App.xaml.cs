using GameMaker.UX.ViewModels;
using GameMaker.UX.ViewModels.HomeView;
using GameMaker.UX.Views.HomeView;
using GameMaker.UX.Views.MainWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace GameMaker.AppMain;

public partial class App
{
    #region Fields

    private static ServiceProvider? ServiceProvider { get; set; }

    #endregion

    public App()
    {
        var services = new ServiceCollection();

        #region Register View Models

        services.AddSingleton<HomeViewViewModel>();

        #endregion

        #region Assign View Models

        services.AddSingleton<HomeView>(provider => new HomeView { DataContext = provider.GetRequiredService<HomeViewViewModel>() });
        // services.AddSingleton<TopBar>(provider => new TopBar { DataContext = provider.GetRequiredService<TopBarViewModel>() });

        #endregion

        #region Register Services

        services.AddSingleton<Func<Type, BaseViewModel>>(provider => viewModelType => (BaseViewModel) provider.GetRequiredService(viewModelType));

        #endregion

        ServiceProvider = services.BuildServiceProvider();

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var startupForm = ServiceProvider?.GetRequiredService<MainWindow>();
        startupForm?.Activate();

        base.OnLaunched(args);

        // m_window = new MainWindow();
        // m_window.Activate();
    }
}
