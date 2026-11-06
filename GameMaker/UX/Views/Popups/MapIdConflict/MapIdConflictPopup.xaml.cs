using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.MapIdConflict;

namespace GameMaker.UX.Views.Popups.MapIdConflict;

public partial class MapIdConflictPopup : IPopupView
{
    #region Properties

    public MapIdConflictViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public MapIdConflictPopup(MapIdConflictViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
