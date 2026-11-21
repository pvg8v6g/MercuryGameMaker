using GameLibrary.Models.Troops;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.TroopsPage;

/// <summary>
/// A spawn point drawn in the troop arena: a tile on the floor, blue for the party and red for random enemies.
/// Party points are numbered, since with a Fixed party spawn point 1 is the leader's.
/// </summary>
public partial class SpawnPointModel(TroopSpawnPoint point, bool isParty, string label) : PropertyChangedUpdater
{
    #region Properties

    public TroopSpawnPoint Point => point;

    public bool IsParty => isParty;

    public bool IsEnemy => !isParty;

    public string Label => label;

    public int X => point.X;

    public int Y => point.Y;

    public string ToolTip => isParty ? $"Party spawn point {label}. Right-click to remove it." : "Random enemy spawn point. Right-click to remove it.";

    #endregion
}
