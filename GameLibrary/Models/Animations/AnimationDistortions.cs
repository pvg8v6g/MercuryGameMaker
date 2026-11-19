using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Animations;

/// <summary>
/// A shockwave an animation sends out from its target, starting on a frame: a ring that bends the map and everyone
/// on it as it spreads from the start radius to the end radius over its frames, weakening until it's gone.
/// X and Y move its middle from the middle of the target. Sizes are unscaled map pixels, like the cells.
/// </summary>
public class AnimationShockwave : PropertyChangedUpdater
{
    #region Properties

    public int Frame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public double X
    {
        get;
        set => SetField(ref field, value);
    }

    public double Y
    {
        get;
        set => SetField(ref field, value);
    }

    public double StartRadius
    {
        get;
        set => SetField(ref field, value);
    }

    public double EndRadius
    {
        get;
        set => SetField(ref field, value);
    } = 120;

    public double Thickness
    {
        get;
        set => SetField(ref field, value);
    } = 24;

    // how far the ring pulls the picture when it starts, in pixels
    public double Strength
    {
        get;
        set => SetField(ref field, value);
    } = 12;

    // 0 to 1: a light edge on the ring
    public double Glow
    {
        get;
        set => SetField(ref field, value);
    }

    // how many frames it spreads out over (60 is a second)
    public int Frames
    {
        get;
        set => SetField(ref field, value);
    } = 30;

    #endregion

    #region Methods

    // the last frame it shows on
    public int LastFrame => Frame + Math.Max(Frames, 1) - 1;

    // the ring this many frames after it started (0 is its first frame), sized for the scale the map is drawn at
    public void ApplyTo(Shockwave wave, int age, double scale)
    {
        var progress = Math.Clamp((double) age / Math.Max(Frames, 1), 0.0, 1.0);
        wave.Radius = (float) ((StartRadius + ((EndRadius - StartRadius) * progress)) * scale);
        wave.Thickness = (float) (Thickness * scale);
        wave.Strength = (float) (Strength * (1.0 - progress) * scale);
        wave.Glow = (float) (Glow * (1.0 - progress));
    }

    #endregion
}

/// <summary>
/// Heat an animation gives off around its target, starting on a frame: shimmering air over an area, a fire spell's
/// flames. It fades in and out over the fade frames. X and Y move its middle from the middle of the target. Sizes are
/// unscaled map pixels, like the cells.
/// </summary>
public class AnimationHeatHaze : PropertyChangedUpdater
{
    #region Properties

    public int Frame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public double X
    {
        get;
        set => SetField(ref field, value);
    }

    public double Y
    {
        get;
        set => SetField(ref field, value);
    } = -24;

    public double Width
    {
        get;
        set => SetField(ref field, value);
    } = 96;

    public double Height
    {
        get;
        set => SetField(ref field, value);
    } = 120;

    // how far it pulls the picture, in pixels
    public double Strength
    {
        get;
        set => SetField(ref field, value);
    } = 3;

    // the size of one ripple, in pixels
    public double RippleSize
    {
        get;
        set => SetField(ref field, value);
    } = 24;

    // how fast it rises, in pixels a second
    public double Speed
    {
        get;
        set => SetField(ref field, value);
    } = 60;

    // how many frames it lasts (60 is a second)
    public int Frames
    {
        get;
        set => SetField(ref field, value);
    } = 60;

    // frames it takes to fade in at the start and out at the end
    public int FadeFrames
    {
        get;
        set => SetField(ref field, value);
    } = 10;

    #endregion

    #region Methods

    // the last frame it shows on
    public int LastFrame => Frame + Math.Max(Frames, 1) - 1;

    // the haze this many frames after it started (0 is its first frame), sized for the scale the map is drawn at.
    // Its position is left to the caller
    public void ApplyTo(HeatHaze haze, int age, double scale)
    {
        var frames = Math.Max(Frames, 1);
        var fade = Math.Max(FadeFrames, 0);
        var fadeIn = fade == 0 ? 1.0 : Math.Clamp((age + 1.0) / fade, 0.0, 1.0);
        var fadeOut = fade == 0 ? 1.0 : Math.Clamp((frames - age) / (double) fade, 0.0, 1.0);
        haze.Width = (float) (Width * scale);
        haze.Height = (float) (Height * scale);
        haze.Strength = (float) (Strength * Math.Min(fadeIn, fadeOut) * scale);
        haze.Scale = (float) (RippleSize * scale);
        haze.Speed = (float) (Speed * scale);
        haze.Feather = (float) (Math.Min(Width, Height) / 4.0 * scale);
    }

    #endregion
}
