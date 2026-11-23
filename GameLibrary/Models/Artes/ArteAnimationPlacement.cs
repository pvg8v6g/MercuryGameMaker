using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Artes;

/// <summary>
/// Where an arte's animation goes for one way its user faces, when the arte moves and rotates one animation instead of
/// using one per direction: mirrored left to right and/or up and down first (if set), then turned about the middle of the character,
/// clockwise in degrees, then moved by the offset (unscaled pixels). The game and the editor's preview both place
/// animations with this, so they always agree.
/// </summary>
public partial class ArteAnimationPlacement : PropertyChangedUpdater
{
    #region Properties

    public double OffsetX
    {
        get;
        set => SetField(ref field, value);
    }

    public double OffsetY
    {
        get;
        set => SetField(ref field, value);
    }

    public double Rotation
    {
        get;
        set => SetField(ref field, value);
    }

    // mirrors the animation left to right (before it's turned)
    public bool Mirror
    {
        get;
        set => SetField(ref field, value);
    }

    // mirrors the animation up and down (before it's turned)
    public bool MirrorVertical
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    // the same turn and mirror without the move, for an animation that plays where its target is (the target animation)
    public ArteAnimationPlacement WithoutOffset() => new() { Rotation = Rotation, Mirror = Mirror, MirrorVertical = MirrorVertical };

    // a spot in the animation (unscaled pixels from the character's middle), mirrored, turned and moved
    public (double X, double Y) Place(double x, double y)
    {
        if (Mirror) x = -x;
        if (MirrorVertical) y = -y;
        if (Rotation == 0) return (x + OffsetX, y + OffsetY);

        var (sin, cos) = Math.SinCos(Rotation * Math.PI / 180.0);
        return ((x * cos) - (y * sin) + OffsetX, (x * sin) + (y * cos) + OffsetY);
    }

    // a cell's own scale and rotation once the animation is mirrored and turned. The renderer scales a picture before
    // it turns it, so a mirror is a negative width (or height), and the cell's own turn goes the other way inside one
    // mirror. Both mirrors together are a half turn, so the cell's turn goes its own way again
    public (double ScaleX, double ScaleY, double Rotate) Turn(double scaleX, double scaleY, double rotate)
    {
        var reversed = Mirror != MirrorVertical;
        return (Mirror ? -scaleX : scaleX, MirrorVertical ? -scaleY : scaleY, reversed ? Rotation - rotate : Rotation + rotate);
    }

    #endregion
}
