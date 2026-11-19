using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Weathers;
using GameLibrary.Utilities;
using GameMaker.UX.Utilities;
using MercuryLibrary.WinUI3Components;
using Windows.UI;

namespace GameMaker.UX.Models.Weathers;

/// <summary>
/// What the weather editor edits: one weather's settings, its sound picked from the BGS folder and its color.
/// A new rain picks rain.wav for its sound when the folder has one. The type changing is passed on, so a map can
/// drop its own weather when it's set back to None.
/// </summary>
public class WeatherEditorModel(WeatherSettings settings, string[] soundFiles, string[] thunderFiles, Action<WeatherType>? typeChanged = null) : PropertyChangedUpdater
{
    #region Constants

    private const string RainSound = "rain.wav";

    #endregion

    #region Properties

    public WeatherSettings Settings => settings;

    public WeatherType[] Types { get; } = Enum.GetValues<WeatherType>();

    public WeatherType Type
    {
        get => settings.Type;
        set
        {
            if (settings.Type == value) return;

            settings.Type = value;
            if (value == WeatherType.Rain && settings.Sound is null && soundFiles.Contains(RainSound, StringComparer.OrdinalIgnoreCase))
            {
                SelectedSound = soundFiles.First(x => string.Equals(x, RainSound, StringComparison.OrdinalIgnoreCase));
            }

            typeChanged?.Invoke(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasWeather));
            OnPropertyChanged(nameof(IsRain));
            OnPropertyChanged(nameof(IsAsh));
            OnPropertyChanged(nameof(PickerColor));
        }
    }

    public bool HasWeather => settings.Type != WeatherType.None;

    public bool IsRain => settings.Type == WeatherType.Rain;

    public bool IsAsh => settings.Type == WeatherType.Ash;

    public string[] SoundFiles { get; } = [AudioFiles.None, .. soundFiles];

    public string SelectedSound
    {
        get => AudioFiles.NameOf(settings.Sound);
        set
        {
            if (value is null || value == SelectedSound) return;

            settings.Sound = AudioFiles.FromName(settings.Sound, value, "BGS");
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSound));
        }
    }

    public bool HasSound => settings.Sound is not null;

    // rain's lightning: the flashes and the thunder after them, from the SFX folder
    public bool Lightning
    {
        get => settings.Lightning;
        set
        {
            if (settings.Lightning == value) return;

            settings.Lightning = value;
            OnPropertyChanged();
        }
    }

    public string[] ThunderFiles { get; } = [AudioFiles.None, .. thunderFiles];

    public string SelectedThunder
    {
        get => AudioFiles.NameOf(settings.Thunder);
        set
        {
            if (value is null || value == SelectedThunder) return;

            settings.Thunder = AudioFiles.FromName(settings.Thunder, value, "SFX");
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasThunder));
        }
    }

    public bool HasThunder => settings.Thunder is not null;

    // the picked color, or the kind's own when none is picked
    public Color PickerColor
    {
        get => settings.Color.ToColorOr(WeatherSettings.DefaultColor(settings.Type));
        set
        {
            if (value == PickerColor) return;

            settings.Color = value.ToHex();
            OnPropertyChanged();
        }
    }

    public RelayCommand DefaultColorCommand => field ??= new RelayCommand(() =>
    {
        settings.Color = null;
        OnPropertyChanged(nameof(PickerColor));
    });

    #endregion
}
