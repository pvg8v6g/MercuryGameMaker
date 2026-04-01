using GameLibrary.Enumerations;

namespace GameLibrary.Models.Areas;

public partial class Hitbox : Area
{
    #region Properties

    private int _anchorX;
    public int AnchorX
    {
        get => _anchorX;
        set => SetField(ref _anchorX, value, nameof(RelativeToAnchorX));
    }

    private int _anchorY;
    public int AnchorY
    {
        get => _anchorY;
        set => SetField(ref _anchorY, value, nameof(RelativeToAnchorY));
    }

    public int RelativeToAnchorX => X + AnchorX;
    public int RelativeToAnchorY => Y + AnchorY;

    public GameColor Color
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Collision

    // public boolean collide(Area area) {
    //     return false;
    // }
    //
    // public boolean collide(Hitbox hitbox) {
    //     var dox = getX();
    //     var doy = getY();
    //     var fox = hitbox.getX();
    //     var foy = hitbox.getY();
    //     var collidesOnX = dox < fox + hitbox.width && fox < dox + width;
    //     var collidesOnY = doy < foy + hitbox.height && foy < doy + height;
    //     return collidesOnX && collidesOnY;
    // }

    public bool Collide(Area area)
    {
        return false;
    }

    public bool Collide(Hitbox hitbox)
    {
        return false;
    }

    #endregion
}
