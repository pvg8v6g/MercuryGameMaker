namespace GameLibrary.Models.Movements;

public partial class WaitMovement : Movement
{
    /// <summary>Wait time in milliseconds (1000 = 1 second).</summary>
    public int Wait
    {
        get;
        set => SetField(ref field, value);
    }

    // nothing on the map is affected, so there is no entity to pick
    public override bool HasEntityTarget => false;

    public override string ToString() => $"Wait: {Wait}ms";
}
