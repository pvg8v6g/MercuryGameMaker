using GameMaker.UX.ViewModels.Popups.FrameSpanning;

namespace GameMaker.UX.Views.Popups.FrameSpanning;

public partial class FrameSpanningPopup
{
    #region Properties

    public FrameSpanningPopupViewModel ViewModel { get; }

    #endregion

    public FrameSpanningPopup(FrameSpanningPopupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
