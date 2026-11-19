using MercuryLibrary.Enumerations;
using MercuryLibrary.ImageComponents;
using Windows.UI;

namespace GameLibrary.Models.Animations;

/// <summary>
/// A color tint fading on a picture: its color and blend change straight away, its strength slides to the target a
/// little every update (0 is no tint). The game keeps one per entity, the Animations page one for its preview actor.
/// </summary>
public class TintFade
{
    #region Properties

    public Color Color { get; private set; }

    public BlendMode? BlendMode { get; private set; }

    public double Strength { get; private set; }

    public double TargetStrength { get; private set; }

    public double PerUpdate { get; private set; }

    public bool IsFading => Math.Abs(Strength - TargetStrength) > 0.0001;

    #endregion

    #region Methods

    // a new tint fading to the strength over this many updates. Fading out keeps the color and blend it had, so it
    // doesn't change color on the way out
    public void To(Color color, double strength, BlendMode? blendMode, int updates)
    {
        TargetStrength = Math.Clamp(strength, 0, 1);
        if (TargetStrength > 0 || Strength <= 0)
        {
            Color = color;
            BlendMode = blendMode;
        }

        if (updates <= 0)
        {
            Strength = TargetStrength;
            return;
        }

        PerUpdate = Math.Abs(TargetStrength - Strength) / updates;
    }

    // fades the tint out over this many updates
    public void Out(int updates) => To(Color, 0, BlendMode, updates);

    // once an update
    public void Step()
    {
        if (!IsFading) return;

        var step = Math.Max(PerUpdate, 0.0001);
        Strength = Strength < TargetStrength
            ? Math.Min(TargetStrength, Strength + step)
            : Math.Max(TargetStrength, Strength - step);
    }

    // no tint at all
    public void Clear()
    {
        Strength = 0;
        TargetStrength = 0;
        PerUpdate = 0;
    }

    // puts the tint on the picture as it is now
    public void ApplyTo(PictureBox picture)
    {
        picture.TintColor = Color;
        picture.TintBlendMode = BlendMode;
        picture.TintStrength = (float) Strength;
    }

    #endregion
}
