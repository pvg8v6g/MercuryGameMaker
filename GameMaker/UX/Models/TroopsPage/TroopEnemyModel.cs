using GameLibrary.Models.Fighter;
using GameLibrary.Models.Troops;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.TroopsPage;

public partial class TroopEnemyModel(TroopEnemy troopEnemy, Fighter? enemy) : PropertyChangedUpdater
{
    #region Properties

    public TroopEnemy TroopEnemy => troopEnemy;

    public string Name => enemy?.Name ?? "(Missing Enemy)";

    public CroppedImage? Sprite
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
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

    public bool IsPlaced => X is not null && Y is not null;

    public string PositionText => IsPlaced ? $"{X}, {Y}" : "Not placed";

    #endregion

    #region Private Methods

    private void OnPositionChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(IsPlaced));
        OnPropertyChanged(nameof(PositionText));
    }

    #endregion
}
