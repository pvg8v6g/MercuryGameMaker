using GameLibrary.Commands;
using MercuryLibrary.WinUI3Components;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace GameEngine.UX.ViewModels;

/// <summary>
/// Every scene loads when its view is shown and updates every frame of the game canvas.
/// </summary>
public abstract class BaseViewModel : PropertyChangedUpdater
{
    #region Relays

    public AsyncRelayCommand LoadedCommand => new(LoadedAction);

    // the game canvas calls this every frame on its game loop thread
    public Action<ICanvasAnimatedControl, CanvasAnimatedUpdateEventArgs> UpdateCallback => (_, _) => Update();

    #endregion

    #region Actions

    protected virtual Task LoadedAction()
    {
        return Task.CompletedTask;
    }

    protected virtual void Update()
    {
    }

    #endregion
}
