using GameEngine.Scenes;
using MercuryLibrary.WinUI3Components;

namespace GameEngine.Interface;

/// <summary>
/// A screen drawn over the game: the battle HUD, the menu, a shop. Each one says whether the game stops while it's open
/// (a menu does, the battle HUD never does). They share one look (Interface.xaml), so the HUD and the menus feel like
/// the same interface, the menus just a layer further up.
/// </summary>
public abstract partial class ScreenLayer : PropertyChangedUpdater
{
    #region Properties

    // whether the map, its pages and the battle stand still while this is open
    public abstract bool PausesGame { get; }

    // shown on screen. Changed on the ui thread, read by the game loop to know whether to pause
    public bool IsOpen
    {
        get;
        protected set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    // called from the game loop once an update, open or not, so a layer can open or close itself
    public abstract void Update(GameCamera camera);

    #endregion
}
