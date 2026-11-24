namespace GameLibrary.Models.States;

public partial class State : BaseModel
{
    #region Properties

    // how long it lasts, in ticks of TickInterval frames each (5 ticks at 60 is 5 seconds). With nothing to tick, an
    // interval of 60 makes this plain seconds. zero causes a permanent effect
    public int Ticks
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

    // more than one of it can be on a fighter, up to MaxStacks. A non-stackable state applied again is ignored. All the
    // stacks share one timer: adding a stack restarts it, and they all end together
    public bool Stackable
    {
        get;
        set => SetField(ref field, value);
    }

    public int MaxStacks
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // every state is removed when a battle ends unless it says it lasts
    public bool RemainsAfterBattle
    {
        get;
        set => SetField(ref field, value);
    }

    // taunts, Feared and links end when whoever applied them falls. Poisons and burns keep going
    public bool EndsWhenSourceFalls
    {
        get;
        set => SetField(ref field, value);
    }

    // stun, frozen, sealed, rooted and the like: bosses are immune
    public bool StunLock
    {
        get;
        set => SetField(ref field, value);
    }

    // a stunned fighter can do nothing at all: no walking, turning or artes, and a cast is broken. This lets one that holds
    // this state still use artes where it stands (a rooted or snared fighter, say). Off unless asked for
    public bool CanUseArtes
    {
        get;
        set => SetField(ref field, value);
    }

    // silenced: the holder can't use artes (it can still walk, turn and use items). Unlike Stun-Lock bosses aren't immune
    public bool Silence
    {
        get;
        set => SetField(ref field, value);
    }

    // the holder can't use items while it lasts (it can still walk, turn and use artes)
    public bool NoItems
    {
        get;
        set => SetField(ref field, value);
    }

    // a good state (buffs, wards, regeneration). Anything that removes negative states leaves these alone. Off by default
    public bool Friendly
    {
        get;
        set => SetField(ref field, value);
    }

    // never shows on the HUD
    public bool Hidden
    {
        get;
        set => SetField(ref field, value);
    }

    // flat changes to attributes while it lasts, for each stack (Defense -20 with 3 stacks is -60)
    public Dictionary<Guid, int> AttributeChanges { get; init; } = new();

    // percent changes to attributes while it lasts, for each stack (Defense -20 is 20% less Defense; 2 stacks is 40% less).
    // Applied after the flat changes, so they scale with the fighter's level
    public Dictionary<Guid, int> AttributePercents { get; init; } = new();

    // how fast the holder walks while it lasts, in percent: 100 is normal, 60 is a slow (Chilled). Several multiply
    public int MoveSpeed
    {
        get;
        set => SetField(ref field, Math.Max(0, value));
    } = 100;

    // the rate it sets for an element while it lasts, like equipment's (100 is no change, 50 takes 50 points off), for each stack
    public Dictionary<Guid, int> ElementChanges { get; init; } = new();

    // states that can't be put on the holder while it has this one (Venom Ward keeps Poisoned off), like equipment's
    public List<Guid> ResistStates { get; init; } = [];

    // the arte activated on the holder every TickInterval frames, owned by whoever applied the state (so it uses their
    // Power). It costs nothing and has no cast or cooldown. Give it the Self scope
    public Guid? TickArte
    {
        get;
        set => SetField(ref field, value);
    }

    // in frames => 60 == 1 second
    public int TickInterval
    {
        get;
        set => SetField(ref field, value);
    } = 60;

    #endregion
}
