using GameMaker.Tasks;
using GameMaker.UX.ViewModels;
using GameMaker.UX.Views.MainWindow;
using GameMaker.UX.Views.Popups.Progress;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.Services.Navigation;

public class NavigationService(Func<Type, BaseViewModel> viewModelFactory, Func<Type, EngineTask> engineTaskFactory)
    : PropertyChangedUpdater, INavigationService
{
    #region Properties

    public BaseViewModel ActivePage
    {
        get;
        set => SetField(ref field, value);
    } = null!;

    public BaseViewModel TopBar
    {
        get;
        set => SetField(ref field, value);
    } = null!;

    #endregion

    #region Functions

    public void SetTopBar<T>() where T : BaseViewModel
    {
        TopBar = viewModelFactory.Invoke(typeof(T));
    }

    public void NavigateTo<T>() where T : BaseViewModel
    {
        ActivePage = viewModelFactory.Invoke(typeof(T));
    }

    public async void ShowProgressPopup<T>(string? label) where T : EngineTask
    {
        var progressPopup = new ProgressPopup
        {
            EngineTask = engineTaskFactory.Invoke(typeof(T)),
            ProgressText = label ?? "Progress",
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await progressPopup.ShowAsync();
    }

    #endregion
}
