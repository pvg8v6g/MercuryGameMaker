using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;

namespace GameLibrary.Services.Media;

public class AudioService(IMediaService mediaService) : IAudioService
{
    #region Fields

    // The maximum number of concurrent one-shot sounds (SFX/footsteps) that may
    // be alive at once. We keep a bounded list of input nodes and dispose each
    // one the moment it finishes, so memory never grows unbounded regardless of
    // how many sounds are triggered over the lifetime of the game.
    private const int MaxConcurrentSounds = 50;

    // Playback is routed through an AudioGraph instead of MediaPlayer because
    // MediaPlayer.PlaybackRate preserves pitch (it time-stretches, only changing
    // tempo). AudioFileInputNode.PlaybackSpeedFactor is a pure resample, so the
    // pitch slider raises/lowers the actual pitch of the sound (as well as its
    // speed, which is the expected resample behaviour).
    private readonly List<AudioFileInputNode> _soundPool = [];
    private readonly Lock _soundPoolLock = new();

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private AudioGraph? _graph;
    private AudioDeviceOutputNode? _output;

    private AudioFileInputNode? _bgm;
    private AudioFileInputNode? _bgs;
    private AudioFileInputNode? _me;

    private CancellationTokenSource? _bgmFade;
    private CancellationTokenSource? _bgsFade;
    private CancellationTokenSource? _meFade;

    #endregion

    #region BGM

    public async void PlayBgm(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopBgm();
        _bgm = await CreateLoopingNode(mediaService.GetBgm(file), volume, pitch);
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

    public async void PlayBgs(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopBgs();
        _bgs = await CreateLoopingNode(mediaService.GetBgs(file), volume, pitch);
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

    public async void PlayMe(string file, double volume = 1.0, double pitch = 1.0)
    {
        StopMe();
        _me = await CreateLoopingNode(mediaService.GetMe(file), volume, pitch);
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
            foreach (var node in _soundPool.ToArray()) DisposeNode(node);
            _soundPool.Clear();
        }
    }

    #endregion

    #region Private methods

    private async Task EnsureGraphAsync()
    {
        if (_graph is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_graph is not null) return;

            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            var graphResult = await AudioGraph.CreateAsync(settings);
            if (graphResult.Status != AudioGraphCreationStatus.Success) return;

            var graph = graphResult.Graph;
            var outputResult = await graph.CreateDeviceOutputNodeAsync();
            if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                graph.Dispose();
                return;
            }

            _output = outputResult.DeviceOutputNode;
            graph.Start();
            _graph = graph;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<AudioFileInputNode?> CreateLoopingNode(Uri source, double volume, double pitch)
    {
        var node = await CreateNode(source, volume, pitch);
        if (node is null) return null;

        node.LoopCount = null; // infinite
        node.Start();
        return node;
    }

    private async void PlayOneShot(Uri source, double volume, double pitch)
    {
        var node = await CreateNode(source, volume, pitch);
        if (node is null) return;

        node.FileCompleted += OnOneShotFinished;

        lock (_soundPoolLock)
        {
            // Bound the pool: if we are already at capacity, dispose the oldest
            // still-playing sound to make room for the new one.
            while (_soundPool.Count >= MaxConcurrentSounds)
            {
                var oldest = _soundPool[0];
                _soundPool.RemoveAt(0);
                DisposeNode(oldest);
            }

            _soundPool.Add(node);
        }

        node.Start();
    }

    private async Task<AudioFileInputNode?> CreateNode(Uri source, double volume, double pitch)
    {
        await EnsureGraphAsync();
        if (_graph is null || _output is null) return null;

        var storageFile = await StorageFile.GetFileFromPathAsync(source.LocalPath);
        var nodeResult = await _graph.CreateFileInputNodeAsync(storageFile);
        if (nodeResult.Status != AudioFileNodeCreationStatus.Success) return null;

        var node = nodeResult.FileInputNode;
        node.Stop();
        node.PlaybackSpeedFactor = pitch;
        node.OutgoingGain = Math.Clamp(volume, 0.0, 1.0);
        node.AddOutgoingConnection(_output);
        return node;
    }

    private static void StopChannel(ref AudioFileInputNode? node)
    {
        if (node is null) return;
        node.Stop();
        node.Dispose();
        node = null;
    }

    private void OnOneShotFinished(AudioFileInputNode sender, object args)
    {
        lock (_soundPoolLock)
        {
            _soundPool.Remove(sender);
        }

        DisposeNode(sender);
    }

    private void DisposeNode(AudioFileInputNode node)
    {
        node.FileCompleted -= OnOneShotFinished;
        node.Stop();
        node.Dispose();
    }

    private static void Fade(AudioFileInputNode? node, double target, double seconds, ref CancellationTokenSource? tokenSource)
    {
        if (node is null) return;

        tokenSource?.Cancel();
        tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var start = node.OutgoingGain;
        var duration = Math.Max(0.0, seconds);

        _ = Task.Run(async () =>
        {
            const int steps = 60;
            if (duration <= 0.0)
            {
                node.OutgoingGain = Math.Clamp(target, 0.0, 1.0);
                return;
            }

            var delay = (int) (duration * 1000.0 / steps);
            for (var i = 1; i <= steps; i++)
            {
                if (token.IsCancellationRequested) return;
                var value = start + (target - start) * (i / (double) steps);
                node.OutgoingGain = Math.Clamp(value, 0.0, 1.0);
                await Task.Delay(Math.Max(1, delay), token).ContinueWith(_ => { });
            }
        }, token);
    }

    #endregion
}
