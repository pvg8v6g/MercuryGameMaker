using GameLibrary.Models;

namespace GameLibrary.Models.States;

public partial class State : BaseModel
{
    #region Properties

    // in frames => 60 == 1 second. zero causes a permanent effect
    public int Ticks
    {
        get;
        set => SetField(ref field, value);
    }

    public int HpDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public double HpPercent
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaDamage
    {
        get;
        set => SetField(ref field, value);
    }

    public double ManaPercent
    {
        get;
        set => SetField(ref field, value);
    }

    // this animation will play every time this state affects the fighter
    public Guid TickAnimation
    {
        get;
        set => SetField(ref field, value);
    }

    public List<Guid> ElementEffect { get; } = [];

    #endregion
}

// public int ticks; // in frames => 60 == 1 second. zero causes a permanent effect
// public int hpDamage;
// public double hpPercent;
// public int manaDamage;
// public double manaPercent;
// public UUID tickAnimation; // this animation will play every time this state affects the fighter
// public final Enumerable<UUID> elementEffect = new Enumerable<>(); // what element of damage does this cause
// public final Enumerable<UUID> removeStates = new Enumerable<>();
// public BlendMode blendMode = null;
