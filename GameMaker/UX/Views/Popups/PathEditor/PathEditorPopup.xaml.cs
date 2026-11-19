using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.MainPage;

namespace GameMaker.UX.Views.Popups.PathEditor;

public partial class PathEditorPopup : IPopupView
{
    #region Properties

    public MainPageViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public PathEditorPopup(MainPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
