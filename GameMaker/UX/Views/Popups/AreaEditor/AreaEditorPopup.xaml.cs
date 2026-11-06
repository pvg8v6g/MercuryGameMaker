using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.MainPage;

namespace GameMaker.UX.Views.Popups.AreaEditor;

public partial class AreaEditorPopup : IPopupView
{
    #region Properties

    public MainPageViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public AreaEditorPopup(MainPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
