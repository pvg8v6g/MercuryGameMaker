using GameLibrary.Models.Media;
using System.Collections.Concurrent;
using GameLibrary.Utilities;
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

    // who's waiting on a sound to be over (PlaySfxToEnd)
    private readonly ConcurrentDictionary<AudioFileInputNode, TaskCompletionSource> _finishers = [];

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private AudioGraph? _graph;
    private AudioDeviceOutputNode? _output;

    private AudioFileInputNode? _bgm;
    private AudioFileInputNode? _bgs;
    private AudioFileInputNode? _me;

    private CancellationTokenSource? _bgmFade;
    private CancellationTokenSource? _bgsFade;
    private CancellationTokenSource? _meFade;

    // goes up on every play and stop, a track that took a while to load only starts if nothing replaced it meanwhile
    private int _bgmRequest;
    private int _bgsRequest;

    private AudioFileInputNode? _weather;
    private CancellationTokenSource? _weatherFade;
    private int _weatherRequest;
    private EqualizerEffectDefinition? _muffle;
    private bool _muffled;

    #endregion

    #region BGM

    public async Task PlayBgm(string file, double volume, double pitch)
    {
        StopBgm();
        var request = _bgmRequest;
        var node = await CreateLoopingNode(mediaService.GetBgm(file), volume, pitch);
        if (request == _bgmRequest) _bgm = node;
        else StopChannel(ref node);
    }

    public void StopBgm()
    {
        _bgmRequest++;
        _bgmFade?.Cancel();
        StopChannel(ref _bgm);
    }

    public void FadeInBgm(double seconds, double volume)
    {
        Fade(_bgm, volume, seconds, ref _bgmFade);
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

    public async Task PlayBgs(string file, double volume, double pitch)
    {
        StopBgs();
        var request = _bgsRequest;
        var node = await CreateLoopingNode(mediaService.GetBgs(file), volume, pitch);
        if (request == _bgsRequest) _bgs = node;
        else StopChannel(ref node);
    }

    public void StopBgs()
    {
        _bgsRequest++;
        _bgsFade?.Cancel();
        StopChannel(ref _bgs);
    }

    public void FadeInBgs(double seconds, double volume)
    {
        Fade(_bgs, volume, seconds, ref _bgsFade);
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

    #region Weather

    // the weather's own loop (rain on the roof), next to the BGS so a map's ambience and the weather play together.
    // Heard from indoors it's muffled: an equalizer cuts its highs, the way walls do
    public async Task PlayWeather(string file, double volume, double pitch)
    {
        StopWeather();
        var request = _weatherRequest;
        var node = await CreateLoopingNode(mediaService.GetBgs(file), volume, pitch);
        if (node is not null && _graph is not null)
        {
            _muffle = MuffleDefinition(_graph);
            node.EffectDefinitions.Add(_muffle);
            if (!_muffled) node.DisableEffectsByDefinition(_muffle);
        }

        if (request == _weatherRequest) _weather = node;
        else StopChannel(ref node);
    }

    public void StopWeather()
    {
        _weatherRequest++;
        _weatherFade?.Cancel();
        StopChannel(ref _weather);
    }

    public void FadeWeather(double seconds, double volume)
    {
        Fade(_weather, volume, seconds, ref _weatherFade);
    }

    public void MuffleWeather(bool muffled)
    {
        _muffled = muffled;
        if (_weather is null || _muffle is null) return;

        if (muffled) _weather.EnableEffectsByDefinition(_muffle);
        else _weather.DisableEffectsByDefinition(_muffle);
    }

    // the lows come through, the mids are softer and the highs (the hiss of rain) are mostly gone
    private static EqualizerEffectDefinition MuffleDefinition(AudioGraph graph)
    {
        var equalizer = new EqualizerEffectDefinition(graph);
        (double Frequency, double Gain)[] bands = [(100, 1.0), (700, 0.5), (2500, 0.126), (8000, 0.126)];
        for (var i = 0; i < equalizer.Bands.Count && i < bands.Length; i++)
        {
            equalizer.Bands[i].FrequencyCenter = bands[i].Frequency;
            equalizer.Bands[i].Gain = bands[i].Gain;
            equalizer.Bands[i].Bandwidth = 2.0;
        }

        return equalizer;
    }

    #endregion

    #region ME

    public async Task PlayMe(string file, double volume, double pitch)
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
        Fade(_me, AudioDefaults.Volume, seconds, ref _meFade);
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

    public void PlaySfx(string file, double volume, double pitch)
    {
        PlayOneShot(mediaService.GetSfx(file), volume, pitch).Forget();
    }

    // finishes when the sound is over (played out or stopped), not when it starts. A missing file finishes at once
    public async Task PlaySfxToEnd(string file, double volume, double pitch)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await PlayOneShot(mediaService.GetSfx(file), volume, pitch, false, done);
        await done.Task;
    }

    // every sound effect stops, which ends any PlaySfxToEnd waiting on one
    public void StopSfx()
    {
        lock (_soundPoolLock)
        {
            foreach (var node in _soundPool.ToArray()) DisposeNode(node);
            _soundPool.Clear();
        }
    }

    // a sound heard through walls (thunder from indoors): the same muffle as the weather's sound
    public void PlayMuffledSfx(string file, double volume, double pitch)
    {
        PlayOneShot(mediaService.GetSfx(file), volume, pitch, true).Forget();
    }

    public void PlayFootstep(string file, double volume, double pitch)
    {
        PlayOneShot(mediaService.GetFootstep(file), volume, pitch).Forget();
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
        StopSfx();
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

    // done (when given) is completed once the sound is over, however it ends: played out, pushed out of the pool, stopped
    // or never started
    private async Task PlayOneShot(Uri source, double volume, double pitch, bool muffled = false, TaskCompletionSource? done = null)
    {
        var node = await CreateNode(source, volume, pitch);
        if (node is null)
        {
            done?.TrySetResult();
            return;
        }

        if (done is not null) _finishers[node] = done;
        if (muffled && _graph is not null) node.EffectDefinitions.Add(MuffleDefinition(_graph));

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
        if (_finishers.TryRemove(node, out var done)) done.TrySetResult();
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
