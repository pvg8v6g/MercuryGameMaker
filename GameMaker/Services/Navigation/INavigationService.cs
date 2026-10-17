using GameLibrary.Tasks;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Navigation;

public interface INavigationService
{
    Frame? TopFrame { get; set; }

    Frame? ActiveFrame { get; set; }

    void SetTopBar<T>() where T : Page;

    void NavigateTo<T>() where T : Page;

    Task ShowProgressPopup<T>(string? label) where T : EngineTask;

    Task ShowAnimationEffectsPopup(AnimationEffectsPopupViewModel viewModel);

    Task<ContentDialogResult> ShowFrameSpanningPopup(FrameSpanningPopupViewModel viewModel);
}
