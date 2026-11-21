using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
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

    // the spots Random enemies start on, each enemy takes a different one picked at random
    public ObservableCollection<TroopSpawnPoint> EnemySpawnPoints
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // Fixed: party member 1 (the leader) starts on party point 1, member 2 on point 2 and so on.
    // Random: each party member starts on a different party point picked at random
    public SpawnMode PartySpawn
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<TroopSpawnPoint> PartySpawnPoints
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
