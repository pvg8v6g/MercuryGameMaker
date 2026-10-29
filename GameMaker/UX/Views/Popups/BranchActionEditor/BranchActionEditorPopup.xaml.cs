using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.BranchActionEditor;

namespace GameMaker.UX.Views.Popups.BranchActionEditor;

public partial class BranchActionEditorPopup : IPopupView
{
    #region Properties

    public BranchActionEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public BranchActionEditorPopup(BranchActionEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
