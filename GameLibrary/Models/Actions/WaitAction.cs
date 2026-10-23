namespace GameLibrary.Models.Actions;

public partial class WaitAction : PageAction
{
    /// <summary>Wait time in frames (60 = 1 second).</summary>
    public int WaitTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Wait: {WaitTime} frames";
}
