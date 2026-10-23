namespace GameLibrary.Models.Actions;

public partial class PlayMediaAction : PageAction
{
    public string? MediaName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Path
    {
        get;
        set => SetField(ref field, value);
    }

    public double Volume
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double Pitch
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public override string ToString() => $"Play Media: {MediaName}";
}
