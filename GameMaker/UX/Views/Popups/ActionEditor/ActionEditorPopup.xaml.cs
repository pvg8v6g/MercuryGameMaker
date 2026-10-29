using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.ActionEditor;

namespace GameMaker.UX.Views.Popups.ActionEditor;

public partial class ActionEditorPopup : IPopupView
{
    #region Properties

    public ActionEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public ActionEditorPopup(ActionEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
