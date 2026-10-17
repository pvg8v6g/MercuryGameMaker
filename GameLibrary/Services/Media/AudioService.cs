using Windows.Media.Core;
using Windows.Media.Playback;

namespace GameLibrary.Services.Media;

public class AudioService(IMediaService mediaService) : IAudioService
{
    #region Fields

    // The maximum number of concurrent one-shot sounds (SFX/footsteps) that may
    // be alive at once. In the old JavaFX engine a fixed thread pool was used and
    // finished sounds were never disposed, which slowly exhausted memory. Here we
    // keep a bounded list of players and dispose each one the moment it finishes
    // (or fails), so memory never grows unbounded regardless of how many sounds
    // are triggered over the lifetime of the game.
    private const int MaxConcurrentSounds = 50;

    private readonly List<MediaPlayer> _soundPool = [];
    private readonly Lock _soundPoolLock = new();

    private MediaPlayer? _bgm;
    private MediaPlayer? _bgs;
    private MediaPlayer? _me;

    private CancellationTokenSource? _bgmFade;
    private CancellationTokenSource? _bgsFade;
    private CancellationTokenSource? _meFade;

    #endregion

    #region BGM

    public void PlayBgm(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopBgm();
        _bgm = PlayLooping(mediaService.GetBgm(file), volume, pitch);
    }

    public void StopBgm()
    {
        _bgmFade?.Cancel();
        StopChannel(ref _bgm);
    }

    public void FadeInBgm(double seconds)
    {
        Fade(_bgm, 0.8, seconds, ref _bgmFade);
    }

    public void FadeOutBgm(double seconds)
    {
        Fade(_bgm, 0.0, seconds, ref _bgmFade);
    }

    public bool BgmPlaying()
    {
        return _bgm is not null;
    }

    #endregion

    #region BGS

    public void PlayBgs(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopBgs();
        _bgs = PlayLooping(mediaService.GetBgs(file), volume, pitch);
    }

    public void StopBgs()
    {
        _bgsFade?.Cancel();
        StopChannel(ref _bgs);
    }

    public void FadeInBgs(double seconds)
    {
        Fade(_bgs, 0.8, seconds, ref _bgsFade);
    }

    public void FadeOutBgs(double seconds)
    {
        Fade(_bgs, 0.0, seconds, ref _bgsFade);
    }

    public bool BgsPlaying()
    {
        return _bgs is not null;
    }

    #endregion

    #region ME

    public void PlayMe(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopMe();
        _me = PlayLooping(mediaService.GetMe(file), volume, pitch);
    }

    public void StopMe()
    {
        _meFade?.Cancel();
        StopChannel(ref _me);
    }

    public void FadeInMe(double seconds)
    {
        Fade(_me, 0.8, seconds, ref _meFade);
    }

    public void FadeOutMe(double seconds)
    {
        Fade(_me, 0.0, seconds, ref _meFade);
    }

    public bool MePlaying()
    {
        return _me is not null;
    }

    #endregion

    #region SFX

    public void PlaySfx(string file, double volume = 1.0, double pitch = 1.0)
    {
        PlayOneShot(mediaService.GetSfx(file), volume, pitch);
    }

    public void PlayFootstep(string file, double volume = 1.0, double pitch = 1.0)
    {
        PlayOneShot(mediaService.GetFootstep(file), volume, pitch);
    }

    #endregion

    #region Utility

    public double DistanceVolume(double sourceX, double sourceY, double targetX, double targetY, double volume)
    {
        var distance = Math.Abs(sourceX - targetX) + Math.Abs(sourceY - targetY);
        var percentDecrease = distance * 0.52083325;
        var result = volume * ((100.0 - percentDecrease) / 100.0);
        return Math.Max(0.0, result);
    }

    public void StopAllMedia()
    {
        StopBgm();
        StopBgs();
        StopMe();

        lock (_soundPoolLock)
        {
            foreach (var player in _soundPool.ToArray()) DisposePlayer(player);
            _soundPool.Clear();
        }
    }

    #endregion

    #region Private methods

    private static MediaPlayer PlayLooping(Uri source, double volume, double pitch)
    {
        var player = new MediaPlayer
        {
            Source = MediaSource.CreateFromUri(source),
            Volume = Math.Clamp(volume, 0.0, 1.0),
            IsLoopingEnabled = true
        };
        player.PlaybackSession.PlaybackRate = pitch;
        player.Play();
        return player;
    }

    private static void StopChannel(ref MediaPlayer? player)
    {
        if (player is null) return;
        player.Pause();
        player.Dispose();
        player = null;
    }

    private void PlayOneShot(Uri source, double volume, double pitch)
    {
        var player = new MediaPlayer
        {
            Source = MediaSource.CreateFromUri(source),
            Volume = Math.Clamp(volume, 0.0, 1.0),
            IsLoopingEnabled = false
        };
        player.PlaybackSession.PlaybackRate = pitch;
        player.MediaEnded += OnOneShotFinished;
        player.MediaFailed += OnOneShotFailed;

        lock (_soundPoolLock)
        {
            // Bound the pool: if we are already at capacity, dispose the oldest
            // still-playing sound to make room for the new one.
            while (_soundPool.Count >= MaxConcurrentSounds)
            {
                var oldest = _soundPool[0];
                _soundPool.RemoveAt(0);
                DisposePlayer(oldest);
            }

            _soundPool.Add(player);
        }

        player.Play();
    }

    private void OnOneShotFinished(MediaPlayer sender, object args)
    {
        RemoveFromPool(sender);
    }

    private void OnOneShotFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        RemoveFromPool(sender);
    }

    private void RemoveFromPool(MediaPlayer player)
    {
        lock (_soundPoolLock)
        {
            _soundPool.Remove(player);
        }

        DisposePlayer(player);
    }

    private void DisposePlayer(MediaPlayer player)
    {
        player.MediaEnded -= OnOneShotFinished;
        player.MediaFailed -= OnOneShotFailed;
        player.Dispose();
    }

    private static void Fade(MediaPlayer? player, double target, double seconds, ref CancellationTokenSource? tokenSource)
    {
        if (player is null) return;

        tokenSource?.Cancel();
        tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var start = player.Volume;
        var duration = Math.Max(0.0, seconds);

        _ = Task.Run(async () =>
        {
            const int steps = 60;
            if (duration <= 0.0)
            {
                player.Volume = Math.Clamp(target, 0.0, 1.0);
                return;
            }

            var delay = (int) (duration * 1000.0 / steps);
            for (var i = 1; i <= steps; i++)
            {
                if (token.IsCancellationRequested) return;
                var value = start + (target - start) * (i / (double) steps);
                player.Volume = Math.Clamp(value, 0.0, 1.0);
                await Task.Delay(Math.Max(1, delay), token).ContinueWith(_ => { });
            }
        }, token);
    }

    #endregion
}
