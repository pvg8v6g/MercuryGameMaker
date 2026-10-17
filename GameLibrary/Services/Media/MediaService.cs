using GameLibrary.Enumerations;
using GameLibrary.Services.Location;

namespace GameLibrary.Services.Media;

public class MediaService(ILocationService locationService) : IMediaService
{
    #region Paths

    public string GetBgmPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Media", "BGM");
    }

    public string GetBgsPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Media", "BGS");
    }

    public string GetMePath()
    {
        return Path.Combine(locationService.GameDirectory!, "Media", "ME");
    }

    public string GetSfxPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Media", "SFX");
    }

    public string GetFootstepsPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Media", "Footsteps");
    }

    #endregion

    #region Accessors

    public Uri GetBgm(string file)
    {
        return new Uri(Path.Combine(GetBgmPath(), file), UriKind.Absolute);
    }

    public Uri GetBgs(string file)
    {
        return new Uri(Path.Combine(GetBgsPath(), file), UriKind.Absolute);
    }

    public Uri GetMe(string file)
    {
        return new Uri(Path.Combine(GetMePath(), file), UriKind.Absolute);
    }

    public Uri GetSfx(string file)
    {
        return new Uri(Path.Combine(GetSfxPath(), file), UriKind.Absolute);
    }

    public Uri GetFootstep(string file)
    {
        return new Uri(Path.Combine(GetFootstepsPath(), file), UriKind.Absolute);
    }

    public string[] GetMediaFiles(Cache cache)
    {
        var path = cache switch
        {
            Cache.Media => GetBgmPath(),
            Cache.Sfx => GetSfxPath(),
            Cache.Footsteps => GetFootstepsPath(),
            _ => throw new ArgumentOutOfRangeException(nameof(cache), cache, null)
        };

        if (!Directory.Exists(path)) return [];
        return Directory.GetFiles(path).Select(Path.GetFileName).Where(x => x is not null).ToArray()!;
    }

    #endregion
}
