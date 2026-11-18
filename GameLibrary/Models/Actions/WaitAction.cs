namespace GameLibrary.Models.Actions;

public partial class WaitAction : PageAction
{
    /// <summary>Wait time in milliseconds (1000 = 1 second), rounded to the nearest game update (about 16.7 ms).</summary>
    public int WaitTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Wait: {WaitTime}ms";
}
