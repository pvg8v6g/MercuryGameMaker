using GameLibrary.Services.Location;
using GameMaker.Services.Navigation;
using GameMaker.Tasks;

namespace GameMaker.UX.ViewModels.HomeView;

public partial class HomeViewViewModel(ILocationService locationService, INavigationService navigationService) : BaseViewModel
{
    #region Properties

    public INavigationService NavigationService => navigationService;

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        locationService.CreateMercuryGameDirectory();
        await locationService.CreateGameDirectory();

        var appDirectory = AppContext.BaseDirectory;
        var gameMakerGraphicsPath = Path.Combine(appDirectory, "Graphics");
        locationService.SetGameMakerGraphicsDirectory(gameMakerGraphicsPath);
        await navigationService.ShowProgressPopup<LoadDataTask>("Loading Game Data");
        NavigationService.SetTopBar<Views.TopBar.TopBar>();
    }

    #endregion
}
