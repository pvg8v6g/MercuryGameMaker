using GameLibrary.Commands;
using GameLibrary.Models;

namespace GameMaker.UX.ViewModels;

public class BaseViewModel : PropertyChangedUpdater
{
    #region Properties

    public BaseModel? CopiedModel { get; set; }

    #endregion

    #region Actions

    public RelayCommand LoadedCommand => new(LoadedAction);

    protected virtual void LoadedAction()
    {
    }

    #endregion
}
