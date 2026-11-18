using GameLibrary.Enumerations;

namespace GameLibrary.Models.Areas;

public partial class Hitbox : Area
{
    #region Properties

    public int AnchorX
    {
        get;
        set => SetField(ref field, value, nameof(RelativeToAnchorX));
    }

    public int AnchorY
    {
        get;
        set => SetField(ref field, value, nameof(RelativeToAnchorY));
    }

    public int RelativeToAnchorX => X + AnchorX;

    public int RelativeToAnchorY => Y + AnchorY;

    public GameColor Color
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
