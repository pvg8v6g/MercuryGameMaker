using GameMaker.UX.ViewModels.Popups.AnimationEffects;

namespace GameMaker.UX.Views.Popups.AnimationEffects;

public partial class AnimationEffectsPopup
{
    #region Properties

    public AnimationEffectsPopupViewModel ViewModel { get; }

    #endregion

    public AnimationEffectsPopup(AnimationEffectsPopupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
