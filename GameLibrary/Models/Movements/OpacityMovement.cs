namespace GameLibrary.Models.Movements;

public partial class OpacityMovement : Movement
{
    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    /// <summary>Transition time in milliseconds (1000 = 1 second).</summary>
    public int TranslateTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Set Opacity: {Opacity}";
}
