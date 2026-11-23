using MercuryLibrary.WinUI3Components;

namespace GameEngine.Interface;

public enum HudPopupKind
{
    Damage,
    Critical,
    Heal,
    Miss
}

/// <summary>
/// A number (or "Miss") that pops up over a fighter's head when a hit lands, floats up and fades away. In scaled map
/// pixels on the layer the camera moves. Made and changed on the ui thread only.
/// </summary>
public partial class HudPopup(string text, double x, double y, HudPopupKind kind) : PropertyChangedUpdater
{
    #region Constants

    // how many updates it shows for, and how far it floats up in that time
    public const int Lifetime = 50;

    private const double Rise = 30;

    #endregion

    #region Properties

    public string Text => text;

    public HudPopupKind Kind => kind;

    public bool IsDamage => kind == HudPopupKind.Damage;

    public bool IsCritical => kind == HudPopupKind.Critical;

    public bool IsHeal => kind == HudPopupKind.Heal;

    public bool IsMiss => kind == HudPopupKind.Miss;

    // centred on the spot: the text is laid out in a box this wide
    public double X => x - 40;

    public double Y { get; private set; } = y - 24;

    public double Opacity { get; private set; } = 1;

    public int Age { get; private set; }

    public bool IsDone => Age >= Lifetime;

    #endregion

    #region Methods

    public void Step()
    {
        Age++;
        Y -= Rise / Lifetime;

        // it holds, then fades over its last third
        Opacity = Math.Clamp((Lifetime - Age) / (Lifetime / 3.0), 0, 1);
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Opacity));
    }

    #endregion
}
