namespace GameLibrary.Models.Movements;

public partial class SpeedMovement : Movement
{
    public int Speed
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Set Speed: {Speed}";
}
