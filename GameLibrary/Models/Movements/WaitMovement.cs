namespace GameLibrary.Models.Movements;

public partial class WaitMovement : Movement
{
    /// <summary>Wait time in milliseconds (1000 = 1 second).</summary>
    public int Wait
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Wait: {Wait}ms";
}
