using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.EnemyArteEditor;

namespace GameMaker.UX.Views.Popups.EnemyArteEditor;

public partial class EnemyArteEditorPopup : IPopupView
{
    #region Properties

    public EnemyArteEditorViewModel ViewModel { get; }

    public PopupShell Shell => (PopupShell) Content;

    #endregion

    public EnemyArteEditorPopup(EnemyArteEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
