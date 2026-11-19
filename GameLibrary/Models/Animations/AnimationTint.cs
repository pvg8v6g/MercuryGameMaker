using MercuryLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Animations;

/// <summary>
/// An animation tinting its target on a frame: the color fades in to the strength over the fade frames (a strength of
/// 0 fades a tint out). A tint left on when the animation ends fades out over the same number of frames.
/// </summary>
public class AnimationTint : PropertyChangedUpdater
{
    public int Frame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

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

    public BlendMode? BlendMode
    {
        get;
        set => SetField(ref field, value);
    } = MercuryLibrary.Enumerations.BlendMode.Multiply;

    // frames (updates) the fade takes, 0 is straight away
    public int FadeFrames
    {
        get;
        set => SetField(ref field, value);
    } = 10;
}
