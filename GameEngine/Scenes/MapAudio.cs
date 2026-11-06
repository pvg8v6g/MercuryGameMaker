using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Media;
using GameLibrary.Services.Media;

namespace GameEngine.Scenes;

/// <summary>
/// Plays each map's music (BGM) and background sound (BGS). A track the next map shares keeps playing through the
/// transfer without restarting, anything else fades out with the screen and the new track fades in with the new map.
/// </summary>
public class MapAudio(IAudioService audioService)
{
    #region Constants

    private const double SameTolerance = 0.001;

    #endregion

    #region Fields

    private AudioFile? _bgm;
    private AudioFile? _bgs;

    #endregion

    #region Methods

    // the player is leaving for the next map, whatever that map doesn't share fades out
    public void Leave(GameMap next, double seconds)
    {
        if (!IsSame(_bgm, Track(next.Bgm)))
        {
            audioService.FadeOutBgm(seconds);
            _bgm = null;
        }

        if (!IsSame(_bgs, Track(next.Bgs)))
        {
            audioService.FadeOutBgs(seconds);
            _bgs = null;
        }
    }

    public async Task Play(GameMap map, double seconds)
    {
        await Task.WhenAll(PlayBgm(Track(map.Bgm), seconds), PlayBgs(Track(map.Bgs), seconds));
    }

    #endregion

    #region Private Methods

    private async Task PlayBgm(AudioFile? track, double seconds)
    {
        if (track is null)
        {
            audioService.StopBgm();
            _bgm = null;
            return;
        }

        if (IsSame(_bgm, track)) return;

        _bgm = track;
        await audioService.PlayBgm(track.Name!, 0.0, track.Pitch);
        audioService.FadeInBgm(seconds, track.Volume);
    }

    private async Task PlayBgs(AudioFile? track, double seconds)
    {
        if (track is null)
        {
            audioService.StopBgs();
            _bgs = null;
            return;
        }

        if (IsSame(_bgs, track)) return;

        _bgs = track;
        await audioService.PlayBgs(track.Name!, 0.0, track.Pitch);
        audioService.FadeInBgs(seconds, track.Volume);
    }

    // a map with no file picked plays nothing
    private static AudioFile? Track(AudioFile? file) => string.IsNullOrEmpty(file?.Name) ? null : file;

    private static bool IsSame(AudioFile? playing, AudioFile? next)
    {
        if (playing is null || next is null) return playing is null && next is null;

        return playing.Name == next.Name
               && Math.Abs(playing.Volume - next.Volume) < SameTolerance
               && Math.Abs(playing.Pitch - next.Pitch) < SameTolerance;
    }

    #endregion
}
