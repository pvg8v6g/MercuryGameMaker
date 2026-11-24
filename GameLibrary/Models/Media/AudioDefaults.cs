namespace GameLibrary.Models.Media;

/// <summary>
/// The volume and pitch a new sound starts with. The game's own numbers (<see cref="Models.GameData.GameSettings"/>,
/// set on the Terms page) are copied here whenever they change or a game loads, so models and pages that make a new
/// sound can read them without reaching for the settings.
/// </summary>
public static class AudioDefaults
{
    public const double StandardVolume = 0.8;

    public const double StandardPitch = 1.0;

    public static double Volume { get; set; } = StandardVolume;

    public static double Pitch { get; set; } = StandardPitch;
}
