using GameMaker.Tasks;
using GameMaker.UX.Views.MainWindow;
using GameMaker.UX.Views.Popups.Progress;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Navigation;

public class NavigationService(Func<Type, EngineTask> engineTaskFactory) : PropertyChangedUpdater, INavigationService
{
    #region Properties

    public Frame? TopFrame
    {
        get;
        set => SetField(ref field, value);
    }

    public Frame? ActiveFrame
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Functions

    public void SetTopBar<T>() where T : Page
    {
        TopFrame?.Navigate(typeof(T));
    }

    public void NavigateTo<T>() where T : Page
    {
        if (ActiveFrame?.Content is T) return;
        ActiveFrame?.Navigate(typeof(T));
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
