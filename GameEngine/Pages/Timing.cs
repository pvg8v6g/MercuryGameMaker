using GameLibrary.Services.GameData;

namespace GameEngine.Pages;

/// <summary>
/// Turns milliseconds into game updates. The game updates 60 times a second, about 16.7 ms each, so a time is rounded
/// to the nearest update, at most about 8 ms off. Anything above 0 waits at least one update.
/// </summary>
public static class Timing
{
    public static int Updates(this IGameDataService gameDataService, int milliseconds)
    {
        return milliseconds <= 0 ? 0 : Math.Max(1, (int) Math.Round(milliseconds / gameDataService.FrameRate));
    }
}
