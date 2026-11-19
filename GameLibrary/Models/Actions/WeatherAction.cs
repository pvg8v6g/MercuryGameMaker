using GameLibrary.Enumerations;
using GameLibrary.Models.Media;
using GameLibrary.Models.Weathers;

namespace GameLibrary.Models.Actions;

/// <summary>
/// Set Weather: changes the world's weather, which stays on from map to map until another Set Weather changes it.
/// The old weather fades out and the new one fades in over the fade time, None clears it. A map with its own weather
/// (ash over a volcano) keeps it while you're on that map.
/// </summary>
public partial class WeatherAction : PageAction
{
    public WeatherSettings Weather
    {
        get;
        set => SetField(ref field, value);
    } = new() { Type = WeatherType.Rain, Sound = new AudioFile { Name = "rain.wav", Path = "BGS" } };

    /// <summary>Fade time in milliseconds (1000 = 1 second).</summary>
    public int FadeTime
    {
        get;
        set => SetField(ref field, value);
    } = 2000;

    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Set Weather: {Weather}";
}
