using MercuryLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class BlendModeMovement : Movement
{
    public BlendMode BlendMode
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>Transition time in milliseconds (1000 = 1 second).</summary>
    public int BlendTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Set Blend: {BlendMode}";
}
