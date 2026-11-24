namespace GameLibrary.Models.Movements;

public partial class WaitMovement : Movement
{
    /// <summary>Wait time in milliseconds (1000 = 1 second).</summary>
    public int Wait
    {
        get;
        set => SetField(ref field, value);
    }

    // a random wait: each time the step runs it waits anywhere from Wait to MaxWait
    public bool Random
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The longest a random wait can be, in milliseconds.</summary>
    public int MaxWait
    {
        get;
        set => SetField(ref field, value);
    }

    // nothing on the map is affected, so there is no entity to pick
    public override bool HasEntityTarget => false;

    public override string ToString() => Random ? $"Wait: {Wait}-{Math.Max(Wait, MaxWait)}ms (Random)" : $"Wait: {Wait}ms";
}
