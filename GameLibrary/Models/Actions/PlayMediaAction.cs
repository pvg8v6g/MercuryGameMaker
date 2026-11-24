using GameLibrary.Models.Media;
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
    } = AudioDefaults.Volume;

    public double Pitch
    {
        get;
        set => SetField(ref field, value);
    } = AudioDefaults.Pitch;

    public override string ToString()
    {
        var result = $"Play Media: {MediaName}";
        result += $"\n    Volume: {Volume:P0}, Pitch: {Pitch:P0}";
        return result;
    }
}
