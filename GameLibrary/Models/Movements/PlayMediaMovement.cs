namespace GameLibrary.Models.Movements;

public partial class PlayMediaMovement : Movement
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

    // nothing on the map is affected, so there is no entity to pick
    public override bool HasEntityTarget => false;

    public override string ToString() => $"Play Media: {MediaName}";
}
