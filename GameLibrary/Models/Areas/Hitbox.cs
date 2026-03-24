using GameLibrary.Enumerations;

namespace GameLibrary.Models.Areas;

public partial class Hitbox : Area
{
    #region Properties

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
