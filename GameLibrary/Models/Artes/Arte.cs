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

    // updates after it's used before it can be used again (60 = 1 second), 0 for none. Every arte has its own,
    // they run at the same time, and the battle adds a tiny global one so one key press can't fire two
    public int Cooldown
    {
        get;
        set => SetField(ref field, Math.Max(0, value));
    }

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

    // how an arte that turns with its user shows its animations: one per direction (Id + 1, + 2, + 3), or one
    // animation moved and turned by the placement for each way the user faces
    public DirectionAnimations DirectionAnimations
    {
        get;
        set => SetField(ref field, value);
    }

    // the placements start as plain turns, so one animation already faces the right way before it's moved
    public ArteAnimationPlacement AnimationDown { get; init; } = new();

    public ArteAnimationPlacement AnimationLeft { get; init; } = new() { Rotation = 90 };

    public ArteAnimationPlacement AnimationRight { get; init; } = new() { Rotation = 270 };

    public ArteAnimationPlacement AnimationUp { get; init; } = new() { Rotation = 180 };

    public ArteAnimationPlacement AnimationFor(Direction direction) => direction switch
    {
        Direction.Left => AnimationLeft,
        Direction.Right => AnimationRight,
        Direction.Up => AnimationUp,
        _ => AnimationDown
    };

    // the hitboxes facing down, and the only ones for an arte that doesn't turn with its user. They're drawn from the
    // tile the user stands on (the anchor)
    public ObservableCollection<Area> Hitboxes { get; init; } = [];

    // an arte that turns with its user can have its own hitboxes for each other way they face, drawn as they land.
    // An empty set turns the down hitboxes instead
    public ObservableCollection<Area> HitboxesLeft { get; init; } = [];

    public ObservableCollection<Area> HitboxesRight { get; init; } = [];

    public ObservableCollection<Area> HitboxesUp { get; init; } = [];

    // the hitboxes drawn for one way of facing (down for anything that isn't left, right or up)
    public ObservableCollection<Area> HitboxesFor(Direction direction) => direction switch
    {
        Direction.Left => HitboxesLeft,
        Direction.Right => HitboxesRight,
        Direction.Up => HitboxesUp,
        _ => Hitboxes
    };

    // a down hitbox turned about the middle of the anchor tile to face another way. The game uses this for a direction
    // with no hitboxes of its own, and the editor's Fill from Down starts each direction from it.
    // Down to left is (x, y) -> (-y, x), to right (y, -x), to up (-x, -y)
    public static Area TurnedFromDown(Area box, Direction facing, int tile)
    {
        var half = tile / 2.0;
        var (x1, y1) = Turn(box.OffsetX - half, box.OffsetY - half, facing);
        var (x2, y2) = Turn(box.OffsetX + box.Width - half, box.OffsetY + box.Height - half, facing);
        return new Area
        {
            OffsetX = (int) Math.Round(Math.Min(x1, x2) + half),
            OffsetY = (int) Math.Round(Math.Min(y1, y2) + half),
            Width = (int) Math.Round(Math.Abs(x2 - x1)),
            Height = (int) Math.Round(Math.Abs(y2 - y1))
        };
    }

    private static (double x, double y) Turn(double x, double y, Direction facing) => facing switch
    {
        Direction.Left => (-y, x),
        Direction.Right => (y, -x),
        Direction.Up => (-x, -y),
        _ => (x, y)
    };

    // times the user's Power (1 is 1x Power, 0.7 is 70%), negative heals
    public double HpDamage
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

    public List<Guid> Elements { get; init; } = [];

    public ObservableCollection<Entity> Projectiles { get; init; } = [];

    // changes the user's Threat when it's used: positive for a tank's artes, negative for a Threat drop
    public int Threat
    {
        get;
        set => SetField(ref field, value);
    }

    // how many fighters the AI wants to catch with it. Never more than are still standing
    public int TargetsWanted
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 1;

    // when in a fight the AI may use it
    public ArteTiming AiTiming
    {
        get;
        set => SetField(ref field, value);
    }

    // the AI only uses it in a temper (it costs a lot, or leaves the user open)
    public bool Risky
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
