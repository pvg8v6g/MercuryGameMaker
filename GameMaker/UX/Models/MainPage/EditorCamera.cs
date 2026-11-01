using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// The editor's window into the map. X/Y is the world pixel drawn at the top left of the view.
/// </summary>
public partial class EditorCamera : PropertyChangedUpdater
{
    #region Properties

    public double X
    {
        get;
        set => SetField(ref field, value);
    }

    public double Y
    {
        get;
        set => SetField(ref field, value);
    }

    public double ViewWidth
    {
        get;
        set => SetField(ref field, value);
    }

    public double ViewHeight
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    // keeps the view inside the world, a world smaller than the view stays pinned to the top left
    public void Clamp(double worldWidth, double worldHeight)
    {
        X = Math.Clamp(X, 0, Math.Max(0, worldWidth - ViewWidth));
        Y = Math.Clamp(Y, 0, Math.Max(0, worldHeight - ViewHeight));
    }

    public void CenterOn(double worldX, double worldY, double worldWidth, double worldHeight)
    {
        X = worldX - (ViewWidth / 2.0);
        Y = worldY - (ViewHeight / 2.0);
        Clamp(worldWidth, worldHeight);
    }

    #endregion
}
