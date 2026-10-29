namespace GameLibrary.Models.Actions;

public partial class FadeMediaAction : PageAction
{
    /// <summary>Fade time in milliseconds (1000 = 1 second).</summary>
    public int FadeTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Fade Out Media: {FadeTime}ms";
}
