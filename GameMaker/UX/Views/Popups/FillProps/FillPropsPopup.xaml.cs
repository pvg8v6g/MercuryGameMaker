using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.FillProps;

namespace GameMaker.UX.Views.Popups.FillProps;

public partial class FillPropsPopup : IPopupView
{
    #region Properties

    public FillPropsViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public FillPropsPopup(FillPropsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
