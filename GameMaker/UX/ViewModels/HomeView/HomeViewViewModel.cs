using GameLibrary.Services.Location;
using GameMaker.Services.Navigation;
using GameMaker.UX.ViewModels.TopBar;

namespace GameMaker.UX.ViewModels.HomeView;

public class HomeViewViewModel(ILocationService locationService, INavigationService navigationService) : BaseViewModel
{
    #region Properties

    public INavigationService NavigationService => navigationService;

    #endregion

    #region Actions

    protected override void LoadedAction()
    {
        navigationService.SetTopBar<TopBarViewModel>();
        locationService.CreateGameDirectory();
    }

    #endregion
}
