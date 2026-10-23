using GameLibrary.Enumerations;

namespace GameLibrary.Models.Actions;

public partial class ScreenFadeAction : PageAction
{
    public Fade Fade
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>Fade time in milliseconds (1000 = 1 second).</summary>
    public int FadeTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Screen Fade: {Fade}";
}
