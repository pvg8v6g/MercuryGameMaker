using MercuryLibrary.Enumerations;

namespace GameLibrary.Models.Actions;

/// <summary>
/// Entity Tint: lays a color over the entity's picture (only its own pixels) and fades it to the strength over the
/// fade time. A strength of 0 fades the tint back out. The tint stays until another tint changes it.
/// </summary>
public partial class EntityTintAction : PageAction, IEntityTargetAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    // the player (the party leader) instead of an entity
    public bool TargetsPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    // the picked entity as "001: name" for the action list, none for the page's own entity
    public string? TargetName
    {
        get;
        set => SetField(ref field, value);
    }

    // #RRGGBB
    public string Color
    {
        get;
        set => SetField(ref field, value);
    } = "#3CC850";

    // 0 is no tint, 1 the full color
    public double Strength
    {
        get;
        set => SetField(ref field, value);
    } = 0.6;

    // how the color mixes with the picture, none lays it flat over it (a silhouette at full strength)
    public BlendMode? BlendMode
    {
        get;
        set => SetField(ref field, value);
    } = MercuryLibrary.Enumerations.BlendMode.Multiply;

    /// <summary>Fade time in milliseconds (1000 = 1 second), 0 is straight away.</summary>
    public int FadeTime
    {
        get;
        set => SetField(ref field, value);
    } = 500;

    // the page waits until the fade is done
    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = Strength <= 0 ? "Entity Tint: Off" : $"Entity Tint: {Color} at {Strength:0%}";
        if (BlendMode is { } blend && Strength > 0) result += $" ({blend})";
        return result + $"\n    Entity: {this.TargetText()}";
    }
}
