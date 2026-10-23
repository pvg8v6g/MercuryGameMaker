using GameMaker.UX.ViewModels.Popups.EntityEditor;

namespace GameMaker.UX.Views.Popups.EntityEditor;

public partial class EntityEditorPopup
{
    #region Properties

    public EntityEditorViewModel ViewModel { get; }

    #endregion

    public EntityEditorPopup(EntityEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
