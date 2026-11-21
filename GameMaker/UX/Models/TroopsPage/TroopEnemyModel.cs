using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Troops;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.TroopsPage;

/// <summary>
/// An enemy in the troop. Its X and Y are the tile it stands on (its feet); the arena draws the sprite rising up from
/// that tile, centred on it, the way characters stand on tiles on the story maps.
/// </summary>
public partial class TroopEnemyModel(TroopEnemy troopEnemy, Fighter? enemy, int tileSize) : PropertyChangedUpdater
{
    #region Properties

    public TroopEnemy TroopEnemy => troopEnemy;

    public string Name => enemy?.Name ?? "(Missing Enemy)";

    public CroppedImage? Sprite
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPositionChanged();
        }
    }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
    }

    public SpawnMode Spawn
    {
        get => troopEnemy.Spawn;
        set
        {
            if (troopEnemy.Spawn == value) return;
            troopEnemy.Spawn = value;
            OnPropertyChanged();
            OnPositionChanged();
        }
    }

    public int? X
    {
        get => troopEnemy.X;
        set
        {
            troopEnemy.X = value;
            OnPositionChanged();
        }
    }

    public int? Y
    {
        get => troopEnemy.Y;
        set
        {
            troopEnemy.Y = value;
            OnPositionChanged();
        }
    }

    // only a Fixed enemy has a spot of its own to show in the arena
    public bool IsPlaced => Spawn == SpawnMode.Fixed && X is not null && Y is not null;

    public string PositionText => Spawn == SpawnMode.Random ? "Random" : IsPlaced ? $"{X}, {Y}" : "Not placed";

    // where the sprite is drawn: centred on the feet tile, its bottom on the tile's bottom
    public double DrawX => (X ?? 0) + ((tileSize - FrameWidth) / 2.0);

    public double DrawY => (Y ?? 0) + tileSize - FrameHeight;

    private double FrameWidth => Sprite?.Rect?.Width ?? tileSize;

    private double FrameHeight => Sprite?.Rect?.Height ?? tileSize;

    #endregion

    #region Private Methods

    private void OnPositionChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(IsPlaced));
        OnPropertyChanged(nameof(PositionText));
        OnPropertyChanged(nameof(DrawX));
        OnPropertyChanged(nameof(DrawY));
    }

    #endregion
}
