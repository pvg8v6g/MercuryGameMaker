using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// A prop placed on a map. X/Y is its base point, the bottom middle of the picture where it meets the ground,
/// in unscaled map pixels. Everything else (picture, footprint, sorting) comes from the prop in the catalogue.
/// </summary>
public partial class MapProp : PropertyChangedUpdater
{
    #region Properties

    // the catalogue prop this is a copy of
    public Guid PropGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public int X
    {
        get;
        set => SetField(ref field, value);
    }

    public int Y
    {
        get;
        set => SetField(ref field, value);
    }

    // mirrored left to right, so one tree can look like two
    public bool Flip
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
