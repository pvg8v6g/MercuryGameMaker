namespace GameLibrary.Services.Media;

public interface IAudioService
{
    #region BGM

    // finishes once the track has started
    Task PlayBgm(string file, double volume = 1.0, double pitch = 1.0);

    void StopBgm();

    void FadeInBgm(double seconds, double volume = 0.8);

    void FadeOutBgm(double seconds);

    bool BgmPlaying();

    #endregion

    #region BGS

    // finishes once the track has started
    Task PlayBgs(string file, double volume = 1.0, double pitch = 1.0);

    void StopBgs();

    void FadeInBgs(double seconds, double volume = 0.8);

    void FadeOutBgs(double seconds);

    bool BgsPlaying();

    #endregion

    #region ME

    void PlayMe(string file, double volume = 1.0, double pitch = 1.0);

    void StopMe();

    void FadeInMe(double seconds);

    void FadeOutMe(double seconds);

    bool MePlaying();

    #endregion

    #region SFX

    void PlaySfx(string file, double volume = 1.0, double pitch = 1.0);

    void PlayFootstep(string file, double volume = 1.0, double pitch = 1.0);

    #endregion

    #region Utility

    double DistanceVolume(double sourceX, double sourceY, double targetX, double targetY, double volume);

    void StopAllMedia();

    #endregion
}
