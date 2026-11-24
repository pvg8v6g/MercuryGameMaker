using GameLibrary.Utilities;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI;

namespace GameLibrary.Models.Animations;

/// <summary>
/// A bolt of lightning drawn from code, starting on a frame (user, 2026-11-23). It runs from its start to its end, both
/// moved from the middle of the target (the default comes down from the sky onto the target). Every strike flashes in,
/// holds with a flicker and fades out; the bolt re-rolls its shape every few frames so it crackles, and every play of
/// the animation is a different bolt. Sizes are unscaled map pixels, like the cells.
/// </summary>
public class AnimationLightning : PropertyChangedUpdater
{
    #region Properties

    public int Frame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // where it starts and ends, from the middle of the target
    public double StartX
    {
        get;
        set => SetField(ref field, value);
    }

    public double StartY
    {
        get;
        set => SetField(ref field, value);
    } = -300;

    public double EndX
    {
        get;
        set => SetField(ref field, value);
    }

    public double EndY
    {
        get;
        set => SetField(ref field, value);
    }

    // the colour of its glow, as #RRGGBB (the core is always white-hot)
    public string Color
    {
        get;
        set => SetField(ref field, value);
    } = "#6EA0FF";

    // how wide the white-hot core is, in pixels
    public double Thickness
    {
        get;
        set => SetField(ref field, value);
    } = 2;

    // how far the coloured glow spreads from the core, in pixels
    public double Glow
    {
        get;
        set => SetField(ref field, value);
    } = 8;

    // how far it zigzags, as a percent of its length: 0 is a straight line, 20 is a natural bolt, 40 is wild
    public double Jaggedness
    {
        get;
        set => SetField(ref field, value);
    } = 22;

    // how often it forks, as a percent: 0 is never
    public double Branches
    {
        get;
        set => SetField(ref field, value);
    } = 35;

    // frames between new shapes: 1 crackles hard, higher is calmer
    public int Crackle
    {
        get;
        set => SetField(ref field, value);
    } = 2;

    // how many times it strikes over its frames
    public int Strikes
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // the radius of the glow where it lands, in pixels (0 is none)
    public double GroundFlash
    {
        get;
        set => SetField(ref field, value);
    } = 50;

    // how much the whole scene lights up as each strike lands, as a percent (0 is none)
    public double ScreenFlash
    {
        get;
        set => SetField(ref field, value);
    } = 30;

    // how many frames it lasts, every strike included (60 is a second)
    public int Frames
    {
        get;
        set => SetField(ref field, value);
    } = 24;

    #endregion

    #region Methods

    // the last frame it shows on
    public int LastFrame => Frame + Math.Max(Frames, 1) - 1;

    // the bolt this many frames after it started (0 is its first frame), sized for the scale the map is drawn at. Seed
    // makes every play a different bolt. Its start and end are left to the caller
    public void ApplyTo(Lightning bolt, int age, double scale, int seed)
    {
        var strikes = Math.Max(Strikes, 1);
        var length = Math.Max(Math.Max(Frames, 1) / strikes, 1);
        var strike = Math.Min(age / length, strikes - 1);
        var into = age - (strike * length);

        bolt.Color = Color.ToColorOr(Colors.White);
        bolt.Thickness = (float) (Thickness * scale);
        bolt.Glow = (float) (Glow * scale);
        bolt.Jaggedness = (float) (Jaggedness / 100.0);
        bolt.Branches = (float) (Branches / 100.0);
        bolt.GroundFlash = (float) (GroundFlash * scale);
        bolt.Intensity = (float) Envelope(into, length);
        bolt.SceneFlash = into < 2 ? (float) (ScreenFlash / 100.0 * (into == 0 ? 0.6 : 1.0)) : 0f;
        bolt.Seed = seed + (strike * 1000) + (into / Math.Max(Crackle, 1));
    }

    // how bright a strike is: it flashes in over 2 frames, holds with a flicker for a little over half, then fades out
    private static double Envelope(int into, int length)
    {
        if (into == 0) return 0.6;
        if (into == 1) return 1.3;

        var hold = Math.Max(2, (int) (length * 0.55));
        if (into < hold) return into % 3 == 0 ? 0.55 : 1.0;

        return Math.Max(0.0, 1.0 - ((double) (into - hold) / Math.Max(1, length - hold)));
    }

    #endregion
}
