namespace GameLibrary.Models.Movements;

public partial class ThroughMovement : Movement
{
    public bool Disabled
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Through: {(Disabled ? "OFF" : "ON")}";
}
