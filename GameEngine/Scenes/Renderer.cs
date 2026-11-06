using System.Collections.ObjectModel;
using GameEngine.Models;
using GameLibrary.Services.GameData;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Dispatching;

namespace GameEngine.Scenes;

/// <summary>
/// Hands the game canvas its pictures. Sprites are placed on screen through the camera and drawn lowest z first.
/// Rendering happens on the game loop thread, the canvas' list is only swapped on the ui thread.
/// </summary>
public partial class Renderer(IGameDataService gameDataService) : PropertyChangedUpdater
{
    #region Constants

    // far off screen, where pictures that are only there to be loaded are put
    private const int Hidden = -100_000;

    #endregion

    #region Fields

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private PictureBox[] _shown = [];

    #endregion

    #region Properties

    // what the canvas draws, in draw order. Always a new list, never changed once the canvas has it: the canvas
    // draws on its own thread, and clearing and refilling a list it is drawing shows black frames
    public ObservableCollection<PictureBox> Nodes
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    // how black the screen is, 0 shows the game and 1 is fully faded out
    public double Fade
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    public void Render(IEnumerable<SceneSprite> sprites, GameCamera camera)
    {
        // the canvas scales a picture around its center, this keeps the scaled picture's top left on the sprite
        var grow = gameDataService.ScaleFactor - 1.0;

        // z is the floor, everything on a lower floor is drawn first. On the same floor entities sort by their feet
        var ordered = sprites
            .OrderBy(x => x.Picture.Z)
            .ThenBy(x => x.SortY)
            .ToArray();
        foreach (var sprite in ordered)
        {
            sprite.Picture.X = (int) Math.Round(sprite.X - camera.X + (sprite.Width * grow / 2.0));
            sprite.Picture.Y = (int) Math.Round(sprite.Y - camera.Y + (sprite.Height * grow / 2.0));
        }

        SetNodes([.. ordered.Select(x => x.Picture)]);
    }

    // called from the game loop, the screen changes on the ui thread
    public void SetFade(double fade)
    {
        _dispatcherQueue.TryEnqueue(() => Fade = Math.Clamp(fade, 0.0, 1.0));
    }

    // the canvas loads an image the first time it is handed a picture of it, handing it every image of the map at
    // once (off-screen) starts them all loading, so an animation's frames are ready before they are shown
    public void Preload(IEnumerable<string> paths)
    {
        var pictures = paths.Distinct().Select(path => new PictureBox { ImagePath = path, X = Hidden, Y = Hidden, Opacity = 0 }).ToArray();
        if (pictures.Length == 0) return;

        _shown = [];
        _dispatcherQueue.TryEnqueue(() => Nodes = [.. pictures]);
    }

    #endregion

    #region Private Methods

    // the canvas only needs a new list when a picture is added, removed or changes places
    private void SetNodes(PictureBox[] pictures)
    {
        if (pictures.SequenceEqual(_shown)) return;
        _shown = pictures;

        // a new list swapped in whole, never Remove either: the canvas disposes a removed picture's bitmap even
        // when another picture still uses it
        _dispatcherQueue.TryEnqueue(() => Nodes = [.. pictures]);
    }

    #endregion
}
