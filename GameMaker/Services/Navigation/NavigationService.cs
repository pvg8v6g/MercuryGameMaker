using GameLibrary.Tasks;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using GameMaker.UX.ViewModels.Popups.MovementEditor;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;
using GameMaker.UX.Views.MainWindow;
using GameMaker.UX.Views.Popups.AnimationEffects;
using GameMaker.UX.Views.Popups.EntityEditor;
using GameMaker.UX.Views.Popups.FrameSpanning;
using GameMaker.UX.Views.Popups.MovementEditor;
using GameMaker.UX.Views.Popups.MovementRouteEditor;
using GameMaker.UX.Views.Popups.Progress;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Navigation;

public partial class NavigationService(Func<Type, EngineTask> engineTaskFactory) : PropertyChangedUpdater, INavigationService
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

    public async Task ShowProgressPopup<T>(string? label) where T : EngineTask
    {
        var progressPopup = new ProgressPopup
        {
            EngineTask = engineTaskFactory.Invoke(typeof(T)),
            ProgressText = label ?? "Progress",
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await progressPopup.ShowAsync();
    }

    public async Task ShowAnimationEffectsPopup(AnimationEffectsPopupViewModel viewModel)
    {
        var popup = new AnimationEffectsPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await popup.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowFrameSpanningPopup(FrameSpanningPopupViewModel viewModel)
    {
        var popup = new FrameSpanningPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        return await popup.ShowAsync();
    }

    public async Task ShowEntityEditorPopup(EntityEditorViewModel viewModel)
    {
        var popup = new EntityEditorPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await popup.ShowAsync();
    }

    public async Task ShowMovementRouteEditorPopup(MovementRouteEditorViewModel viewModel)
    {
        var popup = new MovementRouteEditorPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await popup.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowMovementEditorPopup(MovementEditorViewModel viewModel)
    {
        var popup = new MovementEditorPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        return await popup.ShowAsync();
    }

    #endregion
}
