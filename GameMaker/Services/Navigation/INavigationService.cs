using GameLibrary.Tasks;
using GameMaker.UX.ViewModels.Popups.ActionEditor;
using GameMaker.UX.ViewModels.Popups.BranchActionEditor;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.ConditionEditor;
using GameMaker.UX.ViewModels.Popups.EnemyArteEditor;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using GameMaker.UX.ViewModels.Popups.MovementEditor;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;
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

    Task ShowEntityEditorPopup(EntityEditorViewModel viewModel);

    Task ShowMovementRouteEditorPopup(MovementRouteEditorViewModel viewModel);

    Task<ContentDialogResult> ShowMovementEditorPopup(MovementEditorViewModel viewModel);

    Task<ContentDialogResult> ShowConditionEditorPopup(ConditionEditorViewModel viewModel);

    Task<ContentDialogResult> ShowActionEditorPopup(ActionEditorViewModel viewModel);

    Task<ContentDialogResult> ShowBranchActionEditorPopup(BranchActionEditorViewModel viewModel);

    Task<ContentDialogResult> ShowEnemyArteEditorPopup(EnemyArteEditorViewModel viewModel);
}
