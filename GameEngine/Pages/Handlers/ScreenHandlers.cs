using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Scenes;
using GameEngine.Services.GameState;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Media;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Entity Opacity: fades the entity (or the player) to the opacity over the time. The page doesn't wait for it.
/// </summary>
public class EntityOpacityHandler(IGameStateService gameStateService, MovementRouteSystem movementRouteSystem) : ActionHandler<EntityOpacityAction>
{
    protected override ActionResult Start(PageRun run, EntityOpacityAction action)
    {
        var entity = action.TargetsPlayer ? gameStateService.Player : gameStateService.Find(run, action.EntityId);
        if (entity is not null) movementRouteSystem.FadeTo(entity, action.Opacity, action.TranslateTime);
        return ActionResult.Done;
    }
}

/// <summary>
/// Screen Fade: fades the screen to black (out) or back to the map (in) over the time, the page waits for it.
/// </summary>
public class ScreenFadeHandler(IGameDataService gameDataService, ScreenFade screenFade) : ActionHandler<ScreenFadeAction>
{
    protected override ActionResult Start(PageRun run, ScreenFadeAction action)
    {
        screenFade.FadeTo(action.Fade == Fade.Out, gameDataService.Updates(action.FadeTime));
        return screenFade.IsFading ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, ScreenFadeAction action) => screenFade.IsFading ? ActionResult.Busy : ActionResult.Done;
}

/// <summary>
/// Scroll Map: slides the camera to the player, an entity (which it then follows) or the middle of a tile (where it
/// stays). Scrolling back to the player hands the camera back to them. The page waits until the camera is there.
/// </summary>
public class ScrollMapHandler(IGameDataService gameDataService, IGameStateService gameStateService, GameCamera camera) : ActionHandler<ScrollMapAction>
{
    // map pixels a tile is drawn at, the camera slides this many unscaled pixels an update
    private const double SlidePixels = 4;

    protected override ActionResult Start(PageRun run, ScrollMapAction action)
    {
        var speed = SlidePixels * gameDataService.ScaleFactor;
        if (action.TargetPlayer && gameStateService.Player is { } player) camera.SlideTo(player, speed);
        else if (action.TargetEntityId is { } guid && gameStateService.MapEntities.FirstOrDefault(x => x.Entity.Guid == guid) is { } entity) camera.SlideTo(entity, speed);
        else
        {
            var tile = gameDataService.GridSize * gameDataService.ScaleFactor;
            camera.SlideTo(new Point((action.TargetX + 0.5) * tile, (action.TargetY + 0.5) * tile), speed);
        }

        return ActionResult.Busy;
    }

    protected override ActionResult Update(PageRun run, ScrollMapAction action) => camera.IsSliding ? ActionResult.Busy : ActionResult.Done;
}

/// <summary>
/// Play Media: the path is the media folder. BGM and BGS take over from the map's music or background sound until a
/// map with its own is entered, ME plays once over the music, anything else (SFX) plays as a sound effect.
/// </summary>
public class PlayMediaHandler(IAudioService audioService, MapAudio mapAudio) : ActionHandler<PlayMediaAction>
{
    protected override ActionResult Start(PageRun run, PlayMediaAction action)
    {
        if (string.IsNullOrEmpty(action.MediaName)) return ActionResult.Done;

        var track = new AudioFile { Name = action.MediaName, Path = action.Path, Volume = action.Volume, Pitch = action.Pitch };
        switch (action.Path?.Trim().ToUpperInvariant())
        {
            case "BGM":
                _ = mapAudio.PlayBgm(track);
                break;
            case "BGS":
                _ = mapAudio.PlayBgs(track);
                break;
            case "ME":
                _ = audioService.PlayMe(action.MediaName, action.Volume, action.Pitch);
                break;
            default:
                audioService.PlaySfx(action.MediaName, action.Volume, action.Pitch);
                break;
        }

        return ActionResult.Done;
    }
}

/// <summary>
/// Fade Media: fades out the music and background sound over the time. The page doesn't wait for it.
/// </summary>
public class FadeMediaHandler(MapAudio mapAudio) : ActionHandler<FadeMediaAction>
{
    protected override ActionResult Start(PageRun run, FadeMediaAction action)
    {
        mapAudio.FadeOut(Math.Max(0, action.FadeTime) / 1000.0);
        return ActionResult.Done;
    }
}

/// <summary>
/// Set Weather: changes the world's weather, the old fades out and the new fades in over the fade time. None clears
/// it. With wait for completion the page holds until the fade is done.
/// </summary>
public class WeatherHandler(IGameDataService gameDataService, MapWeather mapWeather) : ActionHandler<WeatherAction>
{
    protected override ActionResult Start(PageRun run, WeatherAction action)
    {
        var weather = action.Weather.Type == WeatherType.None ? null : action.Weather.Copy();
        mapWeather.Change(weather, gameDataService.Updates(action.FadeTime));
        return action.WaitForCompletion && mapWeather.IsChanging ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, WeatherAction action) => mapWeather.IsChanging ? ActionResult.Busy : ActionResult.Done;
}
