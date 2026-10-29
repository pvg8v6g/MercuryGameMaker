using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.ConditionEditor;

namespace GameMaker.UX.Views.Popups.ConditionEditor;

public partial class ConditionEditorPopup : IPopupView
{
    #region Properties

    public ConditionEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public ConditionEditorPopup(ConditionEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
