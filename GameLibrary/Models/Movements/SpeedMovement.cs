using GameLibrary.Models.Modules;

namespace GameLibrary.Models.Movements;

public partial class SpeedMovement : Movement
{
    public int Speed
    {
        get;
        set => SetField(ref field, value);
    } = MovementModule.DefaultSpeed;

    public override string ToString() => $"Set Speed: {Speed}";
}
