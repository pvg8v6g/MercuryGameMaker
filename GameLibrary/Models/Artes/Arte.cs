using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;

namespace GameLibrary.Models.Artes;

public partial class Arte : BaseModel
{
    #region Properties

    public string? Description
    {
        get;
        set => SetField(ref field, value);
    }

    public int Cost
    {
        get;
        set => SetField(ref field, value);
    }

    public int CastTime
    {
        get;
        set => SetField(ref field, value);
    } = 60;

    public ItemUse ArteUse
    {
        get;
        set => SetField(ref field, value);
    } = ItemUse.BattleOnly;

    public EffectScope EffectScope
    {
        get;
        set => SetField(ref field, value);
    } = EffectScope.Enemies;

    public Guid? CasterAnimation
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? TargetAnimation
    {
        get;
        set => SetField(ref field, value);
    }

    public bool DirectionRotate
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Area> Hitboxes { get; init; } = [];

    public int HpDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public int Variance
    {
        get;
        set => SetField(ref field, value);
    }

    public List<Guid> AddStates { get; init; } = [];

    public List<Guid> RemoveStates { get; init; } = [];

    public List<Guid> Elements { get; init; } = [];

    public ObservableCollection<Entity> Projectiles { get; init; } = [];

    #endregion
}
