using GameLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Troops;

public class TroopEnemy : PropertyChangedUpdater
{
    #region Properties

    public Guid EnemyGuid
    {
        get;
        set => SetField(ref field, value);
    }

    // Fixed: starts on X, Y. Random: starts on one of the troop's enemy spawn points, a different one for each enemy
    public SpawnMode Spawn
    {
        get;
        set => SetField(ref field, value);
    }

    // the top left of the tile the enemy stands on (its feet), in unscaled battle map pixels. Only used when Fixed
    public int? X
    {
        get;
        set => SetField(ref field, value);
    }

    public int? Y
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
