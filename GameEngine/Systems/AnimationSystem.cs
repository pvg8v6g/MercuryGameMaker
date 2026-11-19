using System.Collections.Concurrent;
using GameEngine.Models;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Media;
using GameLibrary.Utilities;
using Microsoft.UI;
using MercuryLibrary.ImageComponents;
using Windows.Foundation;

namespace GameEngine.Systems;

/// <summary>
/// Plays animations (made on the Animations page) on entities. An animation is a list of cells per frame: each cell
/// is one square of an animation sheet (5 across) placed around the middle of the entity, with its own scale, turn,
/// colour, opacity and blend, and a sound on the frame it starts. One frame is one update, the way the Animations page
/// plays them. Many animations can play on one entity at once, each goes away after its last frame.
/// Runs after the character images so the animation sits on the character where it was drawn this update.
/// </summary>
public class AnimationSystem(IGameDataService gameDataService, IGraphicsService graphicsService, IAudioService audioService) : IEntitySystem
{
    #region Constants

    // animations draw over the map and everyone on it, under the balloons
    private const int AnimationZ = 9_000;

    // shockwaves and heat bend the map and everyone on it, the animation's own cells stay sharp over them
    private const int DistortionZ = AnimationZ - 1;

    // an animation sheet is this many cells across, the cells are square
    private const int SheetColumns = 5;

    #endregion

    #region Fields

    // full sheet path to the size of one of its cells, in unscaled pixels
    private readonly ConcurrentDictionary<string, double> _cellSizes = [];

    #endregion

    #region Load

    // the animations the map's pages can play are measured with the map, so they start straight away
    public async Task Load(IReadOnlyList<MapEntity> entities)
    {
        var animations = entities
            .SelectMany(x => x.Entity.Pages)
            .SelectMany(x => AllActions(x.Actions))
            .OfType<EntityAnimationAction>()
            .Select(x => x.AnimationId)
            .Distinct();
        foreach (var id in animations) await Measure(Find(id));
    }

    #endregion

    #region Methods

    // the sheets of the animations the map's pages can play, for the map to preload with its other images
    public IEnumerable<string> SheetPaths(IEnumerable<MapEntity> entities)
    {
        return entities
            .SelectMany(x => x.Entity.Pages)
            .SelectMany(x => AllActions(x.Actions))
            .OfType<EntityAnimationAction>()
            .Select(x => Find(x.AnimationId))
            .OfType<Animation>()
            .SelectMany(Sheets)
            .Distinct();
    }

    // starts the animation on the entity from its first frame. An animation whose sheets aren't measured yet (one no
    // page on the map plays) starts once they are, a frame or two later
    public void Play(MapEntity entity, Guid? animationId)
    {
        // the last frame with a cell, a tint, or a shockwave or heat still going
        if (Find(animationId) is not { } animation || animation.LastFrame() is var lastFrame && lastFrame < 1) return;

        var playing = new PlayingAnimation(animation, lastFrame);
        entity.State<AnimationState>().Playing.Add(playing);
        if (Sheets(animation).All(_cellSizes.ContainsKey)) return;

        playing.Waiting = true;
        _ = MeasureThen(animation, playing);
    }

    public bool IsPlaying(MapEntity entity) => entity.State<AnimationState>().Playing.Count > 0;

    // the cells showing this update, for the scene to draw
    public IEnumerable<SceneSprite> Sprites(IEnumerable<MapEntity> entities)
    {
        return entities.SelectMany(x => x.State<AnimationState>().Playing).SelectMany(x => x.Showing);
    }

    #endregion

    #region Update

    public void Update(MapEntity entity)
    {
        var state = entity.State<AnimationState>();
        if (state.Playing.Count == 0) return;
        if (entity.IsErased)
        {
            state.Playing.Clear();
            return;
        }

        var (centerX, centerY) = Center(entity);
        foreach (var playing in state.Playing.ToArray())
        {
            if (playing.Waiting) continue;

            if (++playing.Frame > playing.LastFrame)
            {
                FadeOutTint(entity, playing);
                state.Playing.Remove(playing);
                continue;
            }

            TintTarget(entity, playing);
            ShowFrame(playing, centerX, centerY);
            ShowDistortions(playing, centerX, centerY);
        }
    }

    #endregion

    #region Private Methods

    private void ShowFrame(PlayingAnimation playing, double centerX, double centerY)
    {
        var scale = gameDataService.ScaleFactor;
        playing.Showing.Clear();
        foreach (var effect in playing.Animation.Effects.Where(x => x.Frame == playing.Frame).OrderBy(x => x.Cell))
        {
            // a sound plays on the frame it's on, even on a cell with no picture
            if (effect.Sfx is { Name: { Length: > 0 } sound } sfx) audioService.PlaySfx(sound, sfx.Volume, sfx.Pitch);
            if (string.IsNullOrEmpty(effect.Name)) continue;

            var path = Path.Combine(graphicsService.GetAnimationImagesPath(), effect.Name);
            if (!_cellSizes.TryGetValue(path, out var size)) continue;

            // the canvas scales a picture around its middle: the middle of the cell goes on the effect's spot
            var sprite = playing.Cell(effect.Cell, path, AnimationZ);
            var picture = sprite.Picture;
            picture.Viewport = new Rect(effect.Index % SheetColumns * size, effect.Index / SheetColumns * size, size, size);
            picture.ScaleX = (float) (effect.ScaleX * scale);
            picture.ScaleY = (float) (effect.ScaleY * scale);
            picture.Rotate = effect.Rotate;
            picture.Opacity = (float) effect.Opacity;
            picture.Hue = (float) effect.Hue;
            picture.Contrast = (float) effect.Contrast;
            picture.Saturation = (float) effect.Saturation;
            picture.Brightness = (float) effect.Brightness;
            picture.BlendMode = effect.BlendMode;
            sprite.Width = size;
            sprite.Height = size;
            sprite.X = centerX + (effect.X * scale) - (size * scale / 2.0);
            sprite.Y = centerY + (effect.Y * scale) - (size * scale / 2.0);
            playing.Showing.Add(sprite);
        }
    }

    // the shockwaves and heat going on this frame, around the middle of the target
    private void ShowDistortions(PlayingAnimation playing, double centerX, double centerY)
    {
        var scale = gameDataService.ScaleFactor;
        var frame = playing.Frame;
        foreach (var shockwave in playing.Animation.Shockwaves.Where(x => frame >= x.Frame && frame <= x.LastFrame))
        {
            var sprite = playing.Distortion(shockwave, () => new Shockwave { Z = DistortionZ });
            shockwave.ApplyTo((Shockwave) sprite.Picture, frame - shockwave.Frame, scale);

            // a shockwave is placed by its middle, it has no picture for the renderer to size
            sprite.X = centerX + (shockwave.X * scale);
            sprite.Y = centerY + (shockwave.Y * scale);
            playing.Showing.Add(sprite);
        }

        foreach (var heat in playing.Animation.HeatHazes.Where(x => frame >= x.Frame && frame <= x.LastFrame))
        {
            var sprite = playing.Distortion(heat, () => new HeatHaze { Z = DistortionZ });
            heat.ApplyTo((HeatHaze) sprite.Picture, frame - heat.Frame, scale);

            // heat is placed by its top left
            sprite.X = centerX + ((heat.X - (heat.Width / 2.0)) * scale);
            sprite.Y = centerY + ((heat.Y - (heat.Height / 2.0)) * scale);
            playing.Showing.Add(sprite);
        }
    }

    // the animation's tints on this frame, on the entity it plays on
    private static void TintTarget(MapEntity entity, PlayingAnimation playing)
    {
        foreach (var tint in playing.Animation.TargetTints.Where(x => x.Frame == playing.Frame))
        {
            entity.State<AppearanceState>().Tint.To(tint.Color.ToColorOr(Colors.White), tint.Strength, tint.BlendMode, tint.FadeFrames);
            playing.LastTint = tint;
        }
    }

    // a tint the animation left on fades out when it ends, over the frames it faded in with
    private static void FadeOutTint(MapEntity entity, PlayingAnimation playing)
    {
        if (playing.LastTint is not { Strength: > 0 } tint) return;

        entity.State<AppearanceState>().Tint.Out(tint.FadeFrames);
    }

    // the middle of the character as it's drawn, or of the tile it stands on when it has no picture (scaled map pixels)
    private (double X, double Y) Center(MapEntity entity)
    {
        var scale = gameDataService.ScaleFactor;
        if (entity.State<CharacterImageState>().Sprite is { } sprite) return (sprite.X + (sprite.Width * scale / 2.0), sprite.Y + (sprite.Height * scale / 2.0));

        var tile = gameDataService.GridSize * scale;
        return entity.GetModule<PositionModule>() is { } position ? (position.X + (tile / 2.0), position.Y + (tile / 2.0)) : (0, 0);
    }

    private Animation? Find(Guid? id) => id is { } guid ? gameDataService.Animations.FirstOrDefault(x => x.Guid == guid) : null;

    private IEnumerable<string> Sheets(Animation animation)
    {
        return animation.Effects
            .Select(x => x.Name)
            .OfType<string>()
            .Where(x => x.Length > 0)
            .Distinct()
            .Select(x => Path.Combine(graphicsService.GetAnimationImagesPath(), x));
    }

    private async Task Measure(Animation? animation)
    {
        if (animation is null) return;

        foreach (var path in Sheets(animation).Where(x => !_cellSizes.ContainsKey(x) && File.Exists(x)))
        {
            var (width, _) = await graphicsService.GetImageDimensions(path);
            _cellSizes[path] = width / SheetColumns;
        }
    }

    private async Task MeasureThen(Animation animation, PlayingAnimation playing)
    {
        try
        {
            await Measure(animation);
        }
        finally
        {
            playing.Waiting = false;
        }
    }

    // a page's actions and the actions inside its branches
    private static IEnumerable<PageAction> AllActions(IEnumerable<PageAction> actions)
    {
        foreach (var action in actions)
        {
            yield return action;
            if (action is not BranchAction branch) continue;

            foreach (var inner in AllActions(branch.BranchActions.Concat(branch.ElseActions))) yield return inner;
        }
    }

    #endregion
}
