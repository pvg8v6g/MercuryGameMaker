using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.MapSettings;

namespace GameMaker.UX.Views.Popups.MapSettings;

public partial class MapSettingsPopup : IPopupView
{
    #region Properties

    public MapSettingsViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public MapSettingsPopup(MapSettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
