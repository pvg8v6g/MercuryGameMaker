using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;

namespace GameEngine.Models;

/// <summary>
/// A hitbox while its entity is on the map. The saved area holds the unscaled offset and size,
/// this holds where it is right now in scaled map pixels.
/// </summary>
public class MapHitbox(Area source)
{
    #region Properties

    public Area Source => source;

    public GameColor Color => source is Hitbox hitbox ? hitbox.Color : GameColor.Red;

    // the entity position the hitbox is attached to
    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double OffsetX { get; set; }

    public double OffsetY { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Z { get; set; }

    public double X => OriginX + OffsetX;

    public double Y => OriginY + OffsetY;

    #endregion

    #region Methods

    public bool Collide(MapHitbox hitbox)
    {
        var collidesOnX = X < hitbox.X + hitbox.Width && hitbox.X < X + Width;
        var collidesOnY = Y < hitbox.Y + hitbox.Height && hitbox.Y < Y + Height;
        return collidesOnX && collidesOnY;
    }

    #endregion
}
