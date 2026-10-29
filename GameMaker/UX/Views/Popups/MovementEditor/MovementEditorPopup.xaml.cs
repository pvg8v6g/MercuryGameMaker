using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.MovementEditor;

namespace GameMaker.UX.Views.Popups.MovementEditor;

public partial class MovementEditorPopup : IPopupView
{
    #region Properties

    public MovementEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public MovementEditorPopup(MovementEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
