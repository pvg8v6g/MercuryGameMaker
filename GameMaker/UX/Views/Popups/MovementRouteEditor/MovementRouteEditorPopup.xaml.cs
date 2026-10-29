using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;

namespace GameMaker.UX.Views.Popups.MovementRouteEditor;

public partial class MovementRouteEditorPopup : IPopupView
{
    #region Properties

    public MovementRouteEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public MovementRouteEditorPopup(MovementRouteEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
