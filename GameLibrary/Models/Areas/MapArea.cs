using GameLibrary.Enumerations;
using GameLibrary.Models.Media;

namespace GameLibrary.Models.Areas;

/// <summary>
/// A rectangle on a map. OffsetX/OffsetY are the pixel position on the map.
/// </summary>
public partial class MapArea : Area
{
    #region Properties

    public MapAreaType AreaType
    {
        get;
        set => SetField(ref field, value);
    }

    // the sound played when walking inside a footstep area
    public AudioFile? Footstep
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
