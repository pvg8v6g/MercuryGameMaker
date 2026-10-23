namespace GameLibrary.Models.Movements;

public partial class DirectionFixMovement : Movement
{
    public bool Disabled
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Direction Fix: {(Disabled ? "OFF" : "ON")}";
}
