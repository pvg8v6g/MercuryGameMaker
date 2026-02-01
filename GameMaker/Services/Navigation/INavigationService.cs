using GameMaker.Tasks;
using GameMaker.UX.ViewModels;

namespace GameMaker.Services.Navigation;

public interface INavigationService
{
    BaseViewModel ActivePage { get; set; }

    BaseViewModel TopBar { get; set; }

    void SetTopBar<T>() where T : BaseViewModel;

    void NavigateTo<T>() where T : BaseViewModel;

    void ShowProgressPopup<T>(string? label) where T : EngineTask;
}
