using MercuryLibrary.WinUI3Components;
using GameLibrary.Models.Media;
using MercuryLibrary.Enumerations;

namespace GameLibrary.Models.Animations;

public class AnimationEffect : PropertyChangedUpdater
{
    #region Properties

    public string? Name
    {
        get;
        set => SetField(ref field, value);
    }

    public int Frame
    {
        get;
        set => SetField(ref field, value);
    }

    public int Cell
    {
        get;
        set => SetField(ref field, value);
    }

    public int Index
    {
        get;
        set => SetField(ref field, value);
    }

    public double X
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public double Y
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public double ScaleX
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double ScaleY
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double Hue
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public double Contrast
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public double Saturation
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public double Brightness
    {
        get;
        set => SetField(ref field, value);
    } = 0.0;

    public int Rotate
    {
        get;
        set => SetField(ref field, value);
    } = 0;

    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public BlendMode? BlendMode
    {
        get;
        set => SetField(ref field, value);
    }

    public AudioFile? Sfx
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Overrides

    public override bool Equals(object? obj)
    {
        return obj is AnimationEffect effect && effect.Frame == Frame && effect.Cell == Cell;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    #endregion
}
