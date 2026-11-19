using GameLibrary.Enumerations;
using GameLibrary.Models.Media;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Weathers;

/// <summary>
/// What the weather looks and sounds like: its kind, how much falls and how, the sky and a sound that loops while it
/// lasts. Rain and snow don't show on an interior map (its sound carries on, muffled), ash does.
/// Sizes and speeds are for the map at its normal size, the game scales them with the map.
/// </summary>
public class WeatherSettings : PropertyChangedUpdater
{
    #region Properties

    public WeatherType Type
    {
        get;
        set => SetField(ref field, value);
    }

    // particles in every 100 x 100 pixels
    public double Amount
    {
        get;
        set => SetField(ref field, value);
    } = 6;

    // how fast it falls, 1 is normal
    public double Speed
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // pixels a second it's blown sideways, negative blows it left
    public double Wind
    {
        get;
        set => SetField(ref field, value);
    }

    // how big the particles are, 1 is normal
    public double Size
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // #RRGGBB, none is the kind's own color (blue-grey rain, white snow, dark grey ash)
    public string? Color
    {
        get;
        set => SetField(ref field, value);
    }

    // 0 to 1: how solid the particles are at most
    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    } = 0.75;

    // 0 to 1: the sky darkening under it
    public double Overcast
    {
        get;
        set => SetField(ref field, value);
    }

    // rain splashing on the ground
    public bool Splashes
    {
        get;
        set => SetField(ref field, value);
    } = true;

    // 0 to 1: the share of ash that's glowing embers rising
    public double Embers
    {
        get;
        set => SetField(ref field, value);
    } = 0.15;

    // rain only: the sky flashes now and then, thunder following a moment later
    public bool Lightning
    {
        get;
        set => SetField(ref field, value);
    }

    // about how many seconds between strikes, each one comes a little sooner or later
    public double LightningInterval
    {
        get;
        set => SetField(ref field, value);
    } = 10;

    // 0 to 1: how bright a flash is
    public double LightningBrightness
    {
        get;
        set => SetField(ref field, value);
    } = 0.7;

    // plays after each flash, from the SFX folder. Each one comes a little later or sooner, near or far, and a far one is quieter
    public AudioFile? Thunder
    {
        get;
        set => SetField(ref field, value);
    }

    // loops while the weather lasts (rain on the roof, a blizzard's wind), from the BGS folder
    public AudioFile? Sound
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    // rain and snow fall outside, ash drifts in through any door
    public bool ShowsIndoors => Type == WeatherType.Ash;

    // the kind's own color when none is picked: blue-grey rain, white snow, dark grey ash
    public static Windows.UI.Color DefaultColor(WeatherType type) => type switch
    {
        WeatherType.Snow => Windows.UI.Color.FromArgb(255, 255, 255, 255),
        WeatherType.Ash => Windows.UI.Color.FromArgb(255, 70, 68, 66),
        _ => Windows.UI.Color.FromArgb(255, 200, 215, 235)
    };

    public WeatherSettings Copy()
    {
        return new WeatherSettings
        {
            Type = Type,
            Amount = Amount,
            Speed = Speed,
            Wind = Wind,
            Size = Size,
            Color = Color,
            Opacity = Opacity,
            Overcast = Overcast,
            Splashes = Splashes,
            Embers = Embers,
            Lightning = Lightning,
            LightningInterval = LightningInterval,
            LightningBrightness = LightningBrightness,
            Thunder = Thunder is null ? null : new AudioFile { Name = Thunder.Name, Path = Thunder.Path, Volume = Thunder.Volume, Pitch = Thunder.Pitch },
            Sound = Sound is null ? null : new AudioFile { Name = Sound.Name, Path = Sound.Path, Volume = Sound.Volume, Pitch = Sound.Pitch }
        };
    }

    public override string ToString()
    {
        if (Type == WeatherType.None) return "None";

        var sound = string.IsNullOrEmpty(Sound?.Name) ? string.Empty : $", sound {Sound.Name}";
        var lightning = Type == WeatherType.Rain && Lightning ? ", lightning" : string.Empty;
        return $"{Type}, amount {Amount:0.#}{(Wind != 0 ? $", wind {Wind:0}" : string.Empty)}{lightning}{sound}";
    }

    #endregion
}
