using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;

namespace GameMaker.UX.Views.Popups.MovementRouteEditor;

public partial class MovementRouteEditorPopup
{
    #region Properties

    public MovementRouteEditorViewModel ViewModel { get; }

    #endregion

    public MovementRouteEditorPopup(MovementRouteEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
