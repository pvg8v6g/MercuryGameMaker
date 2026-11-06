using GameLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// Where the player appears on a map, in tiles so it never depends on the grid size or scale.
/// </summary>
public partial class SpawnPoint : PropertyChangedUpdater
{
    #region Properties

    public int TileX
    {
        get;
        set => SetField(ref field, value);
    }

    public int TileY
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    #endregion
}
