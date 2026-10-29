using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.EntityEditor;

namespace GameMaker.UX.Views.Popups.EntityEditor;

public partial class EntityEditorPopup : IPopupView
{
    #region Properties

    public EntityEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public EntityEditorPopup(EntityEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
