using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.MainPage;

namespace GameMaker.UX.Views.Popups.MapLayers;

public partial class MapLayersPopup : IPopupView
{
    #region Properties

    public MainPageViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public MapLayersPopup(MainPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
