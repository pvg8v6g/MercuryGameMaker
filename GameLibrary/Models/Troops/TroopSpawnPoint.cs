using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Troops;

/// <summary>
/// A spot a fighter can start a battle on. X and Y are the top left of the tile the fighter stands on (its feet), in
/// unscaled battle map pixels. The sprite rises up from that tile, so a 48x96 sprite stands on it like on a story map.
/// </summary>
public class TroopSpawnPoint : PropertyChangedUpdater
{
    #region Properties

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

    #endregion
}
