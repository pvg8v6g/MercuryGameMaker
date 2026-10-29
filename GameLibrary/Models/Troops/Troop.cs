using System.Collections.ObjectModel;
using GameLibrary.Models.Media;

namespace GameLibrary.Models.Troops;

public partial class Troop : BaseModel
{
    #region Properties

    public ObservableCollection<TroopEnemy> Enemies
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public bool CanLose
    {
        get;
        set => SetField(ref field, value);
    }

    public bool CanRun
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public AudioFile? Bgm
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
