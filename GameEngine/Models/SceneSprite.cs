using MercuryLibrary.ImageComponents;

namespace GameEngine.Models;

/// <summary>
/// A picture on the map. X/Y is its top left in map pixels and Width/Height is its unscaled size,
/// the renderer turns that into where the canvas draws it.
/// </summary>
public class SceneSprite(PictureBox picture)
{
    #region Properties

    public PictureBox Picture => picture;

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    // entities on the same z are drawn top of the map first, so what is lower down stands in front. This is where
    // their feet are in map pixels, anything else (map images, environments) has none and stays under them
    public double? SortY { get; set; }

    #endregion
}
