using GameLibrary.Services.Location;
using GameMaker.Services.Navigation;

namespace GameMaker.UX.ViewModels.HomeView;

public class HomeViewViewModel(ILocationService locationService, INavigationService navigationService) : BaseViewModel
{
    #region Properties

    public INavigationService NavigationService => navigationService;

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        NavigationService.SetTopBar<Views.TopBar.TopBar>();
        locationService.CreateGameDirectory();

        var appDirectory = AppContext.BaseDirectory;
        var gameMakerGraphicsPath = Path.Combine(appDirectory, "Graphics");
        locationService.SetGameMakerGraphicsDirectory(gameMakerGraphicsPath);

        return Task.CompletedTask;
    }

    #endregion
}
