using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Weathers;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;
using GameLibrary.Utilities;
using MercuryLibrary.ImageComponents;
using Windows.UI;

namespace GameEngine.Scenes;

/// <summary>
/// The weather on the map: the world's (set by Set Weather and kept from map to map) or the map's own (ash over a
/// volcano), which shows whatever the world's weather is. An interior map hides the world's rain and snow, but its
/// sound carries on muffled, as if heard through the walls. Ash drifts in anywhere. A change fades the old weather
/// out and the new one in, a new map shows its weather straight away (the screen is black while it loads).
/// </summary>
public class MapWeather(IGameDataService gameDataService, IGameStateService gameStateService, IAudioService audioService, Renderer renderer)
{
    #region Constants

    // over the map, everyone on it and its light, under animations and balloons
    private const int WeatherZ = 8_000;

    // how loud the world's weather is heard from indoors
    private const double IndoorVolume = 0.6;

    // seconds the sound takes to fade when the map changes
    private const double MapSoundFade = 1.0;

    #endregion

    #region Fields

    private readonly Weather _picture = new() { Z = WeatherZ, Intensity = 0 };
    private SceneSprite? _sprite;
    private GameMap? _map;

    // what's drawn now, and what's next once it has faded out
    private WeatherSettings? _showing;
    private WeatherSettings? _next;
    private bool _switching;

    private double _intensity;
    private double _target;
    private double _step = 1.0;

    // the sound looping now: its file, volume and pitch
    private string? _sound;

    // lightning: updates until the next strike, the flash going now (updates since it struck, -1 is none) and the
    // thunder on its way (updates until it rolls in, -1 is none)
    private int _untilStrike = -1;
    private int _flashAge = -1;
    private bool _doubleFlash;
    private int _untilThunder = -1;
    private double _thunderVolume;

    // a flash: bright, a flicker, bright again, then it dies off. A single flash skips the second peak
    private static readonly (int Age, double Level)[] DoubleFlash = [(0, 1.0), (3, 0.25), (6, 0.85), (10, 0.35), (32, 0.0)];
    private static readonly (int Age, double Level)[] SingleFlash = [(0, 1.0), (4, 0.4), (24, 0.0)];

    #endregion

    #region Properties

    public bool IsChanging => _switching || Math.Abs(_intensity - _target) > 0.0001;

    #endregion

    #region Methods

    // a new map: its own weather or the world's, shown straight away
    public void Enter(GameMap map)
    {
        _map = map;
        _switching = false;
        _next = null;
        var weather = Current(map);
        _showing = Visible(map, weather) ? weather : null;
        Apply(_showing);
        _intensity = _target = _showing is null ? 0 : 1;
        _picture.Intensity = (float) _intensity;
        PlaySound(weather, Muffled(map, weather), MapSoundFade);
    }

    // Set Weather: the world's weather changes over this many updates. None (null) clears it
    public void Change(WeatherSettings? weather, int updates)
    {
        gameStateService.Weather = weather;
        if (_map is not { } map || Own(map) is not null) return;

        var next = Visible(map, weather) ? weather : null;
        _step = 1.0 / Math.Max(1, updates);
        PlaySound(weather, Muffled(map, weather), updates / 60.0);

        // nothing showing: the new weather fades in
        if (_showing is null)
        {
            _showing = next;
            Apply(_showing);
            _intensity = updates <= 0 ? Goal(next) : 0;
            _target = Goal(next);
            return;
        }

        // the same kind takes the new settings and stays on
        if (next is not null && next.Type == _showing.Type)
        {
            _showing = next;
            Apply(_showing);
            _target = 1;
            if (updates <= 0) _intensity = 1;
            return;
        }

        // another kind (or none): the old fades out over half the time, then the new one fades in over the other half
        _next = next;
        _switching = true;
        _step = 1.0 / Math.Max(1, updates / 2);
        _target = 0;
        if (updates <= 0) _intensity = 0;
    }

    // once an update: fades along and keeps the weather put in the world as the camera moves
    public void Update(GameCamera camera)
    {
        if (_intensity < _target) _intensity = Math.Min(_target, _intensity + _step);
        else if (_intensity > _target) _intensity = Math.Max(_target, _intensity - _step);

        if (_switching && _intensity <= 0)
        {
            _switching = false;
            _showing = _next;
            _next = null;
            Apply(_showing);
            _target = Goal(_showing);
        }

        UpdateLightning();
        _picture.Intensity = (float) _intensity;
        _picture.ScrollX = (float) camera.X;
        _picture.ScrollY = (float) camera.Y;

        // the renderer takes the camera off, this puts the weather on the whole screen
        _sprite ??= new SceneSprite(_picture);
        _sprite.X = camera.X;
        _sprite.Y = camera.Y;
    }

    public IEnumerable<SceneSprite> Sprites()
    {
        if (_showing is not null && _intensity > 0 && _sprite is not null) yield return _sprite;
    }

    #endregion

    #region Private Methods

    // the rain's lightning strikes now and then: the screen flashes where the rain shows, the thunder follows (muffled
    // indoors, where the flash isn't seen)
    private void UpdateLightning()
    {
        var weather = _map is { } map ? Current(map) : null;
        if (weather is not { Type: WeatherType.Rain, Lightning: true } || _map is null)
        {
            _untilStrike = -1;
            if (_flashAge >= 0) renderer.SetFlash(0);
            _flashAge = -1;
            return;
        }

        var seen = _showing is { Lightning: true } && _intensity > 0;
        if (_untilStrike < 0) _untilStrike = NextStrike(weather);
        if (--_untilStrike <= 0)
        {
            _untilStrike = NextStrike(weather);
            _flashAge = seen ? 0 : -1;
            _doubleFlash = Random.Shared.NextDouble() < 0.6;

            // near or far: thunder comes a quarter of a second to two seconds behind, a far one quieter
            var distance = Random.Shared.NextDouble();
            _untilThunder = weather.Thunder is { Name: { Length: > 0 } } ? (int) (15 + (distance * 105)) : -1;
            _thunderVolume = 1.0 - (distance * 0.5);
        }

        if (_flashAge >= 0)
        {
            var level = Level(_doubleFlash ? DoubleFlash : SingleFlash, _flashAge++);
            renderer.SetFlash(level * Math.Clamp(weather.LightningBrightness, 0, 1) * _intensity);
            if (level <= 0) _flashAge = -1;
        }

        if (_untilThunder >= 0 && --_untilThunder < 0 && weather.Thunder is { Name: { Length: > 0 } name } thunder)
        {
            var muffled = Muffled(_map, weather);
            var volume = thunder.Volume * _thunderVolume * (muffled ? IndoorVolume : 1.0);
            if (muffled) audioService.PlayMuffledSfx(name, volume, thunder.Pitch);
            else audioService.PlaySfx(name, volume, thunder.Pitch);
        }
    }

    // half to one and a half times the interval away
    private static int NextStrike(WeatherSettings weather)
    {
        var seconds = Math.Max(1.0, weather.LightningInterval) * (0.5 + Random.Shared.NextDouble());
        return (int) (seconds * 60);
    }

    // how bright the flash is this many updates after it struck, between its steps
    private static double Level((int Age, double Level)[] steps, int age)
    {
        for (var i = 1; i < steps.Length; i++)
        {
            if (age > steps[i].Age) continue;

            var (fromAge, from) = steps[i - 1];
            var (toAge, to) = steps[i];
            return from + ((to - from) * (age - fromAge) / Math.Max(1, toAge - fromAge));
        }

        return 0;
    }

    private static WeatherSettings? Own(GameMap map) => map.Weather is { Type: not WeatherType.None } own ? own : null;

    private WeatherSettings? Current(GameMap map) => Own(map) ?? gameStateService.Weather;

    // the map's own weather always shows, the world's rain and snow stay outside
    private static bool Visible(GameMap map, WeatherSettings? weather)
    {
        if (weather is not { Type: not WeatherType.None }) return false;

        return ReferenceEquals(weather, Own(map)) || !map.Interior || weather.ShowsIndoors;
    }

    // the world's weather heard from inside
    private static bool Muffled(GameMap map, WeatherSettings? weather) => map.Interior && weather is not null && !ReferenceEquals(weather, Own(map));

    private static double Goal(WeatherSettings? weather) => weather is null ? 0 : 1;

    private void Apply(WeatherSettings? weather)
    {
        if (weather is null) return;

        var scale = (float) gameDataService.ScaleFactor;
        _picture.Kind = weather.Type switch
        {
            WeatherType.Snow => WeatherKind.Snow,
            WeatherType.Ash => WeatherKind.Ash,
            _ => WeatherKind.Rain
        };
        _picture.Amount = (float) weather.Amount;
        _picture.Speed = (float) weather.Speed * scale;
        _picture.Wind = (float) weather.Wind * scale;
        _picture.Size = (float) weather.Size * scale;
        _picture.Overcast = (float) weather.Overcast;
        _picture.Splashes = weather.Splashes;
        _picture.Embers = (float) weather.Embers;

        var color = weather.Color.ToColorOr(WeatherSettings.DefaultColor(weather.Type));
        _picture.Color = Color.FromArgb((byte) Math.Clamp(weather.Opacity * 255, 0, 255), color.R, color.G, color.B);
    }

    // the weather's sound: the same one keeps looping (only its muffle and volume change), another one replaces it
    private void PlaySound(WeatherSettings? weather, bool muffled, double seconds)
    {
        audioService.MuffleWeather(muffled);
        var sound = weather?.Sound is { Name: { Length: > 0 } } file ? file : null;
        var key = sound is null ? null : $"{sound.Name}|{sound.Volume}|{sound.Pitch}";
        if (sound is null)
        {
            if (_sound is not null) audioService.FadeWeather(seconds, 0);
            _sound = null;
            return;
        }

        var volume = sound.Volume * (muffled ? IndoorVolume : 1.0);
        if (key == _sound)
        {
            audioService.FadeWeather(seconds, volume);
            return;
        }

        _sound = key;
        _ = StartSound(sound.Name!, sound.Pitch, volume, seconds);
    }

    private async Task StartSound(string name, double pitch, double volume, double seconds)
    {
        await audioService.PlayWeather(name, 0.0, pitch);
        audioService.FadeWeather(seconds, volume);
    }

    #endregion
}
