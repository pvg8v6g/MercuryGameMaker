using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.GameStart;

namespace GameMaker.UX.Views.Popups.GameStart;

public partial class GameStartPopup : IPopupView
{
    #region Properties

    public GameStartViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public GameStartPopup(GameStartViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
