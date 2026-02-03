using GameMaker.Tasks;
using GameMaker.UX.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Navigation;

public interface INavigationService
{
    Frame? TopFrame { get; set; }

    Frame? ActiveFrame { get; set; }

    void SetTopBar<T>() where T : Page;

    void NavigateTo<T>() where T : Page;

    void ShowProgressPopup<T>(string? label) where T : EngineTask;
}
