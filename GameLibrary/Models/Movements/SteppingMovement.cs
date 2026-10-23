namespace GameLibrary.Models.Movements;

public partial class SteppingMovement : Movement
{
    public bool Disabled
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Stepping Animation: {(Disabled ? "OFF" : "ON")}";
}
