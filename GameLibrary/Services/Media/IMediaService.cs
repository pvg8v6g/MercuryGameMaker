using GameLibrary.Enumerations;

namespace GameLibrary.Services.Media;

public interface IMediaService
{
    string GetBgmPath();

    string GetBgsPath();

    string GetMePath();

    string GetSfxPath();

    string GetFootstepsPath();

    Uri GetBgm(string file);

    Uri GetBgs(string file);

    Uri GetMe(string file);

    Uri GetSfx(string file);

    Uri GetFootstep(string file);

    string[] GetMediaFiles(Cache cache);
}
