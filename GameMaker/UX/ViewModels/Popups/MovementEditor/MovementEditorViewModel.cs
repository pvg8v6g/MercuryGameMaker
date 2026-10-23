using System.Collections.ObjectModel;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Movements;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.MovementEditor;

public class MovementEditorViewModel(Movement movement, ObservableCollection<Arte> artes) : PropertyChangedUpdater
{
    #region Properties

    public Movement Movement { get; } = movement;

    public ObservableCollection<Arte> Artes { get; } = artes;

    #endregion
}
