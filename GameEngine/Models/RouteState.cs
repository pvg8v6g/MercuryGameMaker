using GameEngine.Navigation;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Animations;
using MercuryLibrary.Enumerations;

namespace GameEngine.Models;

/// <summary>
/// The movement route an entity is following and how far along it is.
/// </summary>
public class RouteState
{
    // the route being followed, none when the entity isn't on one
    public EntityMovementAction? Route { get; set; }

    public int Index { get; set; }

    // the step being worked on has started (a wait counting, a random direction picked)
    public bool StepStarted { get; set; }

    public Direction Way { get; set; }

    public int WaitUpdates { get; set; }

    // a walk the step is taking: a tile one way, or a path toward or away from something
    public WalkState Walk { get; } = new();
}

/// <summary>
/// How an entity is drawn on top of its character image: see-through, blend and a color tint, changed by routes,
/// pages and animations. Opacity and the tint's strength slide to their targets a little every update.
/// </summary>
public class AppearanceState
{
    public double Opacity { get; set; } = 1.0;

    public double TargetOpacity { get; set; } = 1.0;

    public double OpacityPerUpdate { get; set; }

    public BlendMode? BlendMode { get; set; }

    // the color tint, faded by pages and animations
    public TintFade Tint { get; } = new();
}
