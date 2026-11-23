using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Artes;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.ViewModels.ArtesPage;

// a box drawn over the preview, in preview pixels
public record PreviewBox(double X, double Y, double Width, double Height);

/// <summary>
/// The Artes page's animation preview: the preview fighter standing on the anchor tile the way the game draws them,
/// the hitboxes the game would use for the picked direction, and the caster animation placed the way the game places
/// it (the animation for that direction by Id, or one animation mirrored, turned and moved). It shows the first frame
/// until Play runs it once. Cells only: sounds, shockwaves and heat aren't previewed.
/// </summary>
public class ArteAnimationPreviewViewModel(IGameDataService gameDataService, IGraphicsService graphicsService) : PropertyChangedUpdater
{
    #region Constants

    public double PreviewSize => 336;

    // frames a second, the game's update rate
    private const double FrameLength = 1000.0 / 60.0;

    #endregion

    #region Fields

    private List<PictureBox[]> _frames = [];
    private PictureBox? _fighter;
    private DispatcherTimer? _timer;
    private int _frame;
    private int _version;

    #endregion

    #region Properties

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    public ObservableCollection<PreviewBox> Hitboxes { get; } = [];

    public ObservableCollection<PreviewBox> FighterHitboxes { get; } = [];

    public bool IsPlaying
    {
        get;
        private set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(PlayButtonText));
        }
    }

    public string PlayButtonText => IsPlaying ? "Stop" : "Play";

    public RelayCommand PlayCommand => new(() =>
    {
        if (IsPlaying) Stop();
        else Play();
    });

    // the anchor tile's top left in preview pixels, in the middle of the preview
    private double TileX => Math.Round((PreviewSize - gameDataService.GridSize) / 2.0);

    private double TileY => Math.Round((PreviewSize - gameDataService.GridSize) / 2.0);

    #endregion

    #region Methods

    // everything shown for this arte facing this way. Called whenever anything it shows changes
    public async Task Rebuild(Arte? arte, Direction facing, CroppedImage? fighterImage, IEnumerable<Area>? fighterHitboxes)
    {
        var version = ++_version;
        Stop();

        var tile = gameDataService.GridSize;
        var spriteWidth = fighterImage?.Rect?.Width ?? tile;
        var spriteHeight = fighterImage?.Rect?.Height ?? tile;
        var spriteX = TileX + Math.Round((tile - spriteWidth) / 2.0);
        var spriteY = TileY + Math.Round(tile - spriteHeight);
        var bodies = fighterHitboxes?.ToArray() ?? [];

        _fighter = fighterImage is { ImagePath: { } path, Rect: { } viewport } ? new PictureBox { ImagePath = path, Viewport = viewport, X = (int) spriteX, Y = (int) spriteY, Z = 0 } : null;

        Hitboxes.Clear();
        FighterHitboxes.Clear();
        foreach (var body in bodies) FighterHitboxes.Add(new PreviewBox(spriteX + body.OffsetX, spriteY + body.OffsetY, body.Width, body.Height));

        if (arte is null)
        {
            _frames = [];
            ShowFrame(-1);
            return;
        }

        // the hitboxes the game uses: the direction's own, or the down ones turned (facing up, from the body's top)
        var turn = arte.DirectionRotate ? facing : Direction.Down;
        var drawn = arte.HitboxesFor(turn);
        var turned = turn != Direction.Down && drawn.Count == 0;
        var boxes = turned ? arte.Hitboxes.Select(x => Arte.TurnedFromDown(x, turn, tile)).ToArray() : drawn.ToArray();
        var pushY = turned && turn == Direction.Up && bodies.FirstOrDefault() is { } main ? spriteY + main.OffsetY - TileY : 0;
        foreach (var box in boxes) Hitboxes.Add(new PreviewBox(TileX + box.OffsetX, TileY + box.OffsetY + pushY, box.Width, box.Height));

        // the caster animation plays about the middle of the character's picture, like the game
        var (animation, placement) = CasterAnimation(arte, turn);
        var frames = animation is null ? [] : await Frames(animation, placement, spriteX + (spriteWidth / 2.0), spriteY + (spriteHeight / 2.0));
        if (version != _version) return;

        _frames = frames;
        ShowFrame(-1);
    }

    #endregion

    #region Private Methods

    // the animation for the way the user faces and where it goes, the same choice the battle makes
    private (Animation? Animation, ArteAnimationPlacement? Placement) CasterAnimation(Arte arte, Direction facing)
    {
        var animation = gameDataService.Animations.FirstOrDefault(x => x.Guid == arte.CasterAnimation);
        if (animation is null || !arte.DirectionRotate) return (animation, null);
        if (arte.DirectionAnimations == DirectionAnimations.MoveAndRotate) return (animation, arte.AnimationFor(facing));

        var turned = gameDataService.Animations.FirstOrDefault(x => x.Id == animation.Id + (int) facing);
        return (turned ?? animation, null);
    }

    // every frame's cells as pictures, worked out once so playing only swaps them
    private async Task<List<PictureBox[]>> Frames(Animation animation, ArteAnimationPlacement? placement, double centerX, double centerY)
    {
        var frames = new List<PictureBox[]>();
        var folder = graphicsService.GetAnimationImagesPath();
        for (var frame = 1; frame <= animation.LastFrame(); frame++)
        {
            var cells = new List<PictureBox>();
            foreach (var effect in animation.Effects.Where(x => x.Frame == frame).OrderBy(x => x.Cell))
            {
                if (string.IsNullOrEmpty(effect.Name)) continue;

                var viewport = await graphicsService.GetAnimationViewport(effect.Name, effect.Index);
                var (x, y) = placement?.Place(effect.X, effect.Y) ?? (effect.X, effect.Y);
                var (scaleX, scaleY, rotate) = placement?.Turn(effect.ScaleX, effect.ScaleY, effect.Rotate) ?? (effect.ScaleX, effect.ScaleY, effect.Rotate);
                cells.Add(new PictureBox
                {
                    ImagePath = Path.Combine(folder, effect.Name),
                    Viewport = viewport,
                    X = (int) Math.Round(centerX + x - (viewport.Width / 2.0)),
                    Y = (int) Math.Round(centerY + y - (viewport.Height / 2.0)),
                    Z = effect.Cell,
                    Opacity = (float) effect.Opacity,
                    ScaleX = (float) scaleX,
                    ScaleY = (float) scaleY,
                    Rotate = (float) rotate,
                    Hue = (float) effect.Hue,
                    Contrast = (float) effect.Contrast,
                    Saturation = (float) effect.Saturation,
                    Brightness = (float) effect.Brightness,
                    BlendMode = effect.BlendMode
                });
            }

            frames.Add([.. cells]);
        }

        return frames;
    }

    // the fighter under one frame's cells. Before playing, the first frame with any cells shows, to place it by
    private void ShowFrame(int index)
    {
        var cells = index >= 0 && index < _frames.Count ? _frames[index] : _frames.FirstOrDefault(x => x.Length > 0) ?? [];
        Sprites.Clear();
        if (_fighter is not null) Sprites.Add(_fighter);
        foreach (var cell in cells) Sprites.Add(cell);
    }

    private void Play()
    {
        if (_frames.Count == 0) return;

        _frame = 0;
        IsPlaying = true;
        ShowFrame(_frame);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FrameLength) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, object e)
    {
        if (++_frame < _frames.Count)
        {
            ShowFrame(_frame);
            return;
        }

        Stop();
    }

    private void Stop()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }

        if (!IsPlaying) return;

        IsPlaying = false;
        ShowFrame(-1);
    }

    #endregion
}
