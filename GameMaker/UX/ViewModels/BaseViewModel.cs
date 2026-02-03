using GameLibrary.Commands;
using GameLibrary.Models;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels;

public class BaseViewModel : PropertyChangedUpdater
{
    #region Properties

    public BaseModel? CopiedModel { get; set; }

    #endregion

    #region Actions

    private AsyncRelayCommand? _loadedCommand;
    public AsyncRelayCommand LoadedCommand => _loadedCommand ??= new(LoadedAction);

    protected virtual Task LoadedAction()
    {
        return Task.CompletedTask;
    }

    #endregion
}
