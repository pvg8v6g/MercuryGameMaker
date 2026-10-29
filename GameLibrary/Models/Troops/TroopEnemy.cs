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
