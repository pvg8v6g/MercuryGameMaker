using GameMaker.UX.ViewModels.Popups.MovementEditor;

namespace GameMaker.UX.Views.Popups.MovementEditor;

public partial class MovementEditorPopup
{
    #region Properties

    public MovementEditorViewModel ViewModel { get; }

    #endregion

    public MovementEditorPopup(MovementEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
