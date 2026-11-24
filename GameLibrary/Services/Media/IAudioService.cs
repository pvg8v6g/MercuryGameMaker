namespace GameLibrary.Services.Media;

public interface IAudioService
{
    #region BGM

    // finishes once the track has started
    Task PlayBgm(string file, double volume, double pitch);

    void StopBgm();

    void FadeInBgm(double seconds, double volume);

    void FadeOutBgm(double seconds);

    bool BgmPlaying();

    #endregion

    #region BGS

    // finishes once the track has started
    Task PlayBgs(string file, double volume, double pitch);

    void StopBgs();

    void FadeInBgs(double seconds, double volume);

    void FadeOutBgs(double seconds);

    bool BgsPlaying();

    #endregion

    #region Weather

    // the weather's own looping sound from the BGS folder, plays next to the BGS. Finishes once it has started
    Task PlayWeather(string file, double volume, double pitch);

    void StopWeather();

    // fades the weather's sound to the volume (0 is silent)
    void FadeWeather(double seconds, double volume);

    // heard from indoors: the highs are cut, like through walls. Kept for the next weather sound too
    void MuffleWeather(bool muffled);

    #endregion

    #region ME

    Task PlayMe(string file, double volume, double pitch);

    void StopMe();

    void FadeInMe(double seconds);

    void FadeOutMe(double seconds);

    bool MePlaying();

    #endregion

    #region SFX

    void PlaySfx(string file, double volume, double pitch);

    // finishes when the sound is over (played out or stopped), not when it starts. A missing file finishes at once
    Task PlaySfxToEnd(string file, double volume, double pitch);

    // every sound effect stops
    void StopSfx();

    // a sound heard through walls (thunder from indoors), its highs cut like the weather's muffled sound
    void PlayMuffledSfx(string file, double volume, double pitch);

    void PlayFootstep(string file, double volume, double pitch);

    #endregion

    #region Utility

    double DistanceVolume(double sourceX, double sourceY, double targetX, double targetY, double volume);

    void StopAllMedia();

    #endregion
}
