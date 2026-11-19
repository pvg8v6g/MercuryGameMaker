using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.MainPage;

namespace GameMaker.UX.Views.Popups.PropEditor;

public partial class PropEditorPopup : IPopupView
{
    #region Properties

    public MainPageViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public PropEditorPopup(MainPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
