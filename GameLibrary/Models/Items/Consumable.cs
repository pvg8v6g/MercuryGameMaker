using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;

namespace GameLibrary.Models.Items;

public partial class Consumable : Item
{
    #region Properties

    public EffectScope EffectScope
    {
        get;
        set => SetField(ref field, value);
    } = EffectScope.Allies;

    public ItemUse ItemUse
    {
        get;
        set => SetField(ref field, value);
    } = ItemUse.Anywhere;

    public bool ClearAllStates
    {
        get;
        set => SetField(ref field, value);
    }

    public int HpDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public int HpPercent
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaPercent
    {
        get;
        set => SetField(ref field, value);
    }

    public int Variance
    {
        get;
        set => SetField(ref field, value);
    } = 10;

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

    public ObservableCollection<Area> Hitboxes { get; init; } = [];

    public Dictionary<Guid, int> AttributeChanges { get; init; } = new();

    public List<Guid> AddStates { get; init; } = [];

    // the percent chance each added state has of going on. A state that isn't listed always does (100)
    public Dictionary<Guid, int> StateChances { get; init; } = new();

    public List<Guid> RemoveStates { get; init; } = [];

    // removes every state that isn't Friendly (poisons, stuns, debuffs) and leaves the good ones. Never a fall:
    // Incapacitated is only removed by ticking it in Remove States
    public bool RemoveNegativeStates
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
