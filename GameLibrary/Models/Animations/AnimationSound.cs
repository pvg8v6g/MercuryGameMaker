using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Animations;

/// <summary>
/// A sound an animation plays on a frame, on its own: it needs no cell, so an animation can be nothing but sounds
/// (a shield clink, a buff chime). Name is a file in the SFX folder.
/// </summary>
public class AnimationSound : PropertyChangedUpdater
{
    #region Properties

    public int Frame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public string? Name
    {
        get;
        set => SetField(ref field, value);
    }

    public double Volume
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double Pitch
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    #endregion
}
