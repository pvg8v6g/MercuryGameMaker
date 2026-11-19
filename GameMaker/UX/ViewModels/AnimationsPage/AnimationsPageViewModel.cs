using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Media;
using GameLibrary.Utilities;
using GameLibrary.Utilities.ComponentModels;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameMaker.Services.Navigation;
using GameMaker.UX.Models.AnimationsPage;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using MercuryLibrary.ImageComponents;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace GameMaker.UX.ViewModels.AnimationsPage;

public class AnimationsPageViewModel(
    IGameDataService gameDataService,
    IGraphicsService graphicsService,
    IJsonService jsonService,
    INavigationService navigationService,
    IAudioService audioService,
    IMediaService mediaService)
    : BaseViewModel<Animation>(jsonService)
{
    #region Fields

    private AnimationEffect? _draggedEffect;
    private PictureBox? _draggedSprite;
    private Point _dragStart;
    private double _dragOriginX;
    private double _dragOriginY;

    private DispatcherTimer? _playTimer;
    private int _tempFrame;
    private int _lastFrame;

    // the first actor with a picture, standing facing down where the animation plays (the red square when there's none)
    private PictureBox? _actorSprite;

    // the frame's tint being edited, watched so the actor shows each change
    private AnimationTint? _watchedTint;

    // the frame's shockwave and heat being edited, watched so the preview shows each change
    private AnimationShockwave? _watchedShockwave;
    private AnimationHeatHaze? _watchedHeatHaze;

    // shockwaves and heat are drawn over the actor and under the cells, like the game
    private const int DistortionZ = -1;

    #endregion

    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Animation> EntityCollection => gameDataService.Animations;

    public FileSelection[] AnimationImageFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public int SelectedAnimationImageFilesIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public int SelectedFrame
    {
        get;
        set
        {
            if (!SetField(ref field, value)) return;
            SelectedCell = null;
            RefreshFrameTint();
            RefreshFrameDistortions();
            _ = RebuildSprites();
        }
    } = 1;

    // the animation tints its target on this frame (a poison animation turning the character green)
    public bool TintsTarget
    {
        get => FrameTint is not null;
        set
        {
            if (SelectedEntity is null || value == TintsTarget) return;

            if (value) SelectedEntity.TargetTints.Add(new AnimationTint { Frame = SelectedFrame });
            else SelectedEntity.TargetTints.Remove(FrameTint!);
            RefreshFrameTint();
        }
    }

    public AnimationTint? FrameTint => SelectedEntity?.TargetTints.FirstOrDefault(x => x.Frame == SelectedFrame);

    // a shockwave starts on this frame, sent out from the target
    public bool StartsShockwave
    {
        get => FrameShockwave is not null;
        set
        {
            if (SelectedEntity is null || value == StartsShockwave) return;

            if (value) SelectedEntity.Shockwaves.Add(new AnimationShockwave { Frame = SelectedFrame });
            else SelectedEntity.Shockwaves.Remove(FrameShockwave!);
            RefreshFrameDistortions();
            _ = RebuildSprites();
        }
    }

    public AnimationShockwave? FrameShockwave => SelectedEntity?.Shockwaves.FirstOrDefault(x => x.Frame == SelectedFrame);

    // heat starts shimmering around the target on this frame
    public bool StartsHeatHaze
    {
        get => FrameHeatHaze is not null;
        set
        {
            if (SelectedEntity is null || value == StartsHeatHaze) return;

            if (value) SelectedEntity.HeatHazes.Add(new AnimationHeatHaze { Frame = SelectedFrame });
            else SelectedEntity.HeatHazes.Remove(FrameHeatHaze!);
            RefreshFrameDistortions();
            _ = RebuildSprites();
        }
    }

    public AnimationHeatHaze? FrameHeatHaze => SelectedEntity?.HeatHazes.FirstOrDefault(x => x.Frame == SelectedFrame);

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    public ObservableCollection<CellOutline> CellOutlines { get; } = [];

    private AnimationEffect? SelectedCell
    {
        get;
        set
        {
            if (!SetField(ref field, value)) return;
            OnPropertyChanged(nameof(IsCellSelected));
            UpdateSelectionRect();
        }
    }

    public bool IsCellSelected => SelectedCell is not null;

    public Rect SelectionRect
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool IsDragging
    {
        get;
        private set => SetField(ref field, value);
    }

    public string DragPositionText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    private int AnchorX
    {
        get;
        set => SetField(ref field, value);
    }

    private int AnchorY
    {
        get;
        set => SetField(ref field, value);
    }

    private double AnchorWidth
    {
        get;
        set => SetField(ref field, value);
    }

    private double AnchorHeight
    {
        get;
        set => SetField(ref field, value);
    }

    public Rect AnchorRect
    {
        get;
        private set => SetField(ref field, value);
    }

    private double CanvasWidth
    {
        get;
        set => SetField(ref field, value);
    }

    private double CanvasHeight
    {
        get;
        set => SetField(ref field, value);
    }

    public RelayCommand<Size> CanvasSizeChangedCommand => new(size =>
    {
        CanvasWidth = size.Width;
        CanvasHeight = size.Height;
        UpdateAnchor();
        _ = RebuildSprites();
    });

    public RelayCommand<PointerEventInfo> CanvasPointerPressedCommand => new(OnCanvasPointerPressed);

    public RelayCommand<PointerEventInfo> CanvasPointerMovedCommand => new(OnCanvasPointerMoved);

    public RelayCommand<PointerEventInfo> CanvasPointerReleasedCommand => new(OnCanvasPointerReleased);

    public AsyncRelayCommand FrameEffectsCommand => new(OpenFrameEffects);

    public AsyncRelayCommand FrameSpanningCommand => new(OpenFrameSpanning);

    public RelayCommand PlayCommand => new(TogglePlayback);

    public RelayCommand DeleteCellCommand => new(DeleteSelectedCell);

    public RelayCommand DuplicateCellCommand => new(DuplicateSelectedCell);

    public RelayCommand DeselectCellCommand => new(() => SelectedCell = null);

    public RelayCommand PreviousFrameCommand => new(() =>
    {
        if (SelectedFrame > 1) SelectedFrame -= 1;
    });

    public RelayCommand NextFrameCommand => new(() => SelectedFrame += 1);

    private bool IsPlaying
    {
        get;
        set
        {
            if (!SetField(ref field, value)) return;
            OnPropertyChanged(nameof(PlayButtonText));
        }
    }

    public string PlayButtonText => IsPlaying ? "Stop" : "Play";

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        RefreshAnimationImageFiles();
        await LoadActorSprite();
        UpdateAnchor();
        await RebuildSprites();
    }

    protected override async Task OnSelectedIndexChanged(int index)
    {
        StopPlayback();
        SelectedCell = null;
        SelectedFrame = 1;
        RefreshFrameTint();
        RefreshFrameDistortions();
        await RebuildSprites();
    }

    #endregion

    private void RefreshFrameTint()
    {
        if (_watchedTint is not null) _watchedTint.PropertyChanged -= OnFrameTintChanged;
        _watchedTint = FrameTint;
        if (_watchedTint is not null) _watchedTint.PropertyChanged += OnFrameTintChanged;

        OnPropertyChanged(nameof(TintsTarget));
        OnPropertyChanged(nameof(FrameTint));
        TintActor();
    }

    private void OnFrameTintChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => TintActor();

    private void RefreshFrameDistortions()
    {
        if (_watchedShockwave is not null) _watchedShockwave.PropertyChanged -= OnFrameDistortionChanged;
        _watchedShockwave = FrameShockwave;
        if (_watchedShockwave is not null) _watchedShockwave.PropertyChanged += OnFrameDistortionChanged;

        if (_watchedHeatHaze is not null) _watchedHeatHaze.PropertyChanged -= OnFrameDistortionChanged;
        _watchedHeatHaze = FrameHeatHaze;
        if (_watchedHeatHaze is not null) _watchedHeatHaze.PropertyChanged += OnFrameDistortionChanged;

        OnPropertyChanged(nameof(StartsShockwave));
        OnPropertyChanged(nameof(FrameShockwave));
        OnPropertyChanged(nameof(StartsHeatHaze));
        OnPropertyChanged(nameof(FrameHeatHaze));
    }

    private void OnFrameDistortionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => _ = RebuildSprites();

    // the shockwaves and heat going on the selected frame, around the middle of the actor the way the game places
    // them around the target. The editor draws at a scale of 1
    private IEnumerable<PictureBox> FrameDistortions()
    {
        if (SelectedEntity is not { } animation) yield break;

        var centerX = AnchorX + (AnchorWidth / 2.0);
        var centerY = AnchorY + (AnchorHeight / 2.0);
        var frame = SelectedFrame;
        foreach (var shockwave in animation.Shockwaves.Where(x => frame >= x.Frame && frame <= x.LastFrame))
        {
            var wave = new Shockwave { Z = DistortionZ, X = (int) (centerX + shockwave.X), Y = (int) (centerY + shockwave.Y) };
            shockwave.ApplyTo(wave, frame - shockwave.Frame, 1.0);
            yield return wave;
        }

        foreach (var heat in animation.HeatHazes.Where(x => frame >= x.Frame && frame <= x.LastFrame))
        {
            var haze = new HeatHaze
            {
                Z = DistortionZ,
                X = (int) (centerX + heat.X - (heat.Width / 2.0)),
                Y = (int) (centerY + heat.Y - (heat.Height / 2.0))
            };
            heat.ApplyTo(haze, frame - heat.Frame, 1.0);
            yield return haze;
        }
    }

    // the actor tinted the way the animation has it on the selected frame: every tint up to the frame played through,
    // each fading a frame at a time the way the game fades it
    private void TintActor()
    {
        if (_actorSprite is null) return;

        var fade = new TintFade();
        if (SelectedEntity is { } animation)
        {
            for (var frame = 1; frame <= SelectedFrame; frame++)
            {
                fade.Step();
                foreach (var tint in animation.TargetTints.Where(x => x.Frame == frame))
                {
                    fade.To(tint.Color.ToColorOr(Colors.White), tint.Strength, tint.BlendMode, tint.FadeFrames);
                }
            }
        }

        fade.ApplyTo(_actorSprite);
    }

    #region Playback

    private void TogglePlayback()
    {
        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        StartPlayback();
    }

    private void StartPlayback()
    {
        if (SelectedEntity is null) return;

        // the last frame with a cell, a tint, or a shockwave or heat still going, the way the game plays it
        _lastFrame = SelectedEntity.LastFrame();
        if (_lastFrame < 1) return;

        _tempFrame = SelectedFrame;
        IsPlaying = true;
        SelectedCell = null;
        SelectedFrame = 1;
        PlayFrameSfx(1);

        _playTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(gameDataService.FrameRate)
        };
        _playTimer.Tick += OnPlayTick;
        _playTimer.Start();
    }

    private void OnPlayTick(object? sender, object e)
    {
        if (SelectedFrame < _lastFrame)
        {
            SelectedFrame += 1;
            PlayFrameSfx(SelectedFrame);
        }
        else
        {
            StopPlayback();
        }
    }

    private void StopPlayback()
    {
        if (_playTimer is not null)
        {
            _playTimer.Stop();
            _playTimer.Tick -= OnPlayTick;
            _playTimer = null;
        }

        if (!IsPlaying) return;

        IsPlaying = false;
        SelectedFrame = _tempFrame;
    }

    private void PlayFrameSfx(int frame)
    {
        if (SelectedEntity is null) return;

        foreach (var sfx in SelectedEntity.Effects
                     .Where(x => x.Frame == frame && x.Sfx is not null)
                     .Select(x => x.Sfx!))
        {
            if (string.IsNullOrEmpty(sfx.Name)) continue;
            audioService.PlaySfx(sfx.Name, sfx.Volume, sfx.Pitch);
        }
    }

    #endregion

    #region Private methods

    private void RefreshAnimationImageFiles()
    {
        AnimationImageFiles = graphicsService.GetAnimationImages();
    }

    // the first actor that has a picture stands in for the target, the way the game centers an animation on the
    // character it plays on. No actor with a picture leaves the red square
    private async Task LoadActorSprite()
    {
        _actorSprite = null;
        AnchorWidth = gameDataService.GridSize;
        AnchorHeight = gameDataService.GridSize;

        var actor = gameDataService.Actors.FirstOrDefault(x => !string.IsNullOrEmpty(x.CharacterName));
        if (actor is null) return;

        var image = await graphicsService.GetCharacter(actor.CharacterName!, actor.CharacterIndex, Direction.Down);
        if (image is not { ImagePath: { } path, Rect: { Width: > 0, Height: > 0 } viewport }) return;

        _actorSprite = new PictureBox { ImagePath = path, Viewport = viewport, Z = 0 };
        AnchorWidth = viewport.Width;
        AnchorHeight = viewport.Height;
        TintActor();
    }

    private void UpdateAnchor()
    {
        AnchorX = (int) ((CanvasWidth - AnchorWidth) / 2.0);
        AnchorY = (int) ((CanvasHeight - AnchorHeight) / 2.0);

        // the actor's picture is the anchor, the square only shows without one
        if (_actorSprite is not null)
        {
            _actorSprite.X = AnchorX;
            _actorSprite.Y = AnchorY;
            AnchorRect = default;
            return;
        }

        AnchorRect = new Rect(AnchorX, AnchorY, AnchorWidth, AnchorHeight);
    }

    private (int X, int Y) EffectToSpritePosition(AnimationEffect effect, Rect? viewport)
    {
        // 0,0 on the animation form maps to the center of the anchor, aligned with
        // the center of the sprite's viewport (not its top-left corner).
        var centerX = AnchorX + AnchorWidth / 2.0 + effect.X;
        var centerY = AnchorY + AnchorHeight / 2.0 + effect.Y;
        var halfWidth = (viewport?.Width ?? 0) / 2.0;
        var halfHeight = (viewport?.Height ?? 0) / 2.0;
        return ((int) (centerX - halfWidth), (int) (centerY - halfHeight));
    }

    private void UpdateSelectionRect()
    {
        if (SelectedCell is null)
        {
            SelectionRect = default;
            return;
        }

        var sprite = Sprites.FirstOrDefault(x => x.Z == SelectedCell.Cell);
        if (sprite?.Viewport is not { } viewport)
        {
            SelectionRect = default;
            return;
        }

        var width = viewport.Width * sprite.ScaleX;
        var height = viewport.Height * sprite.ScaleY;
        var centerX = sprite.X + viewport.Width / 2.0;
        var centerY = sprite.Y + viewport.Height / 2.0;
        SelectionRect = new Rect(centerX - width / 2.0, centerY - height / 2.0, width, height);
    }

    private async Task OpenFrameEffects()
    {
        if (SelectedCell is null) return;

        var popupViewModel = new AnimationEffectsPopupViewModel(SelectedCell, graphicsService, mediaService, audioService);
        await navigationService.ShowAnimationEffectsPopup(popupViewModel);
        await RebuildSprites();
    }

    private async Task OpenFrameSpanning()
    {
        if (SelectedEntity is null) return;

        var popupViewModel = new FrameSpanningPopupViewModel(SelectedFrame);
        var result = await navigationService.ShowFrameSpanningPopup(popupViewModel);

        switch (result)
        {
            case ContentDialogResult.Primary:
                popupViewModel.ApplySpanning(SelectedEntity.Effects);
                break;
            case ContentDialogResult.Secondary:
                SelectedCell = null;
                popupViewModel.DeleteCells(SelectedEntity.Effects);
                break;
            default:
                return;
        }

        await RebuildSprites();
    }

    private void OnCanvasPointerPressed(PointerEventInfo? info)
    {
        if (info is null || SelectedEntity is null) return;

        var position = info.Position;
        var effect = HitTest(position);

        if (effect is not null)
        {
            if (info.Properties.IsRightButtonPressed)
            {
                DeleteEffect(effect);
                return;
            }

            BeginDrag(effect, position);
            return;
        }

        if (info.Properties.IsLeftButtonPressed) _ = PlaceCell();
    }

    private void OnCanvasPointerMoved(PointerEventInfo? info)
    {
        if (info is null || _draggedEffect is null || _draggedSprite is null) return;

        var position = info.Position;
        var dx = position.X - _dragStart.X;
        var dy = position.Y - _dragStart.Y;

        _draggedEffect.X = _dragOriginX + dx;
        _draggedEffect.Y = _dragOriginY + dy;

        var (spriteX, spriteY) = EffectToSpritePosition(_draggedEffect, _draggedSprite.Viewport);
        _draggedSprite.X = spriteX;
        _draggedSprite.Y = spriteY;

        DragPositionText = $"X: {(int) _draggedEffect.X}, Y: {(int) _draggedEffect.Y}";

        UpdateCellOutlines();
        UpdateSelectionRect();
    }

    private void OnCanvasPointerReleased(PointerEventInfo? info)
    {
        _draggedEffect = null;
        _draggedSprite = null;
        IsDragging = false;
    }

    private void BeginDrag(AnimationEffect effect, Point position)
    {
        SelectedCell = effect;
        _draggedEffect = effect;
        _draggedSprite = Sprites.FirstOrDefault(x => x.Z == effect.Cell);
        _dragStart = position;
        _dragOriginX = effect.X;
        _dragOriginY = effect.Y;
        DragPositionText = $"X: {(int) effect.X}, Y: {(int) effect.Y}";
        IsDragging = true;
    }

    private void DeleteSelectedCell()
    {
        if (SelectedCell is null) return;
        DeleteEffect(SelectedCell);
    }

    private void DuplicateSelectedCell()
    {
        if (SelectedEntity is null || SelectedCell is null) return;

        var source = SelectedCell;
        var cell = NextCell();

        var clones = SelectedEntity.Effects
            .Where(x => x.Cell == source.Cell)
            .OrderBy(x => x.Frame)
            .Select(x => new AnimationEffect
            {
                Name = x.Name,
                Frame = x.Frame,
                Cell = cell,
                Index = x.Index,
                X = x.X,
                Y = x.Y,
                ScaleX = x.ScaleX,
                ScaleY = x.ScaleY,
                Rotate = x.Rotate,
                Opacity = x.Opacity,
                Hue = x.Hue,
                Contrast = x.Contrast,
                Saturation = x.Saturation,
                Brightness = x.Brightness,
                BlendMode = x.BlendMode,
                Sfx = x.Sfx
            })
            .ToList();

        if (clones.Count == 0) return;

        foreach (var clone in clones) SelectedEntity.Effects.Add(clone);

        SelectedCell = clones.FirstOrDefault(x => x.Frame == SelectedFrame) ?? clones.First();
        _ = RebuildSprites();
    }

    private void DeleteEffect(AnimationEffect effect)
    {
        if (SelectedEntity is null) return;

        if (ReferenceEquals(SelectedCell, effect)) SelectedCell = null;
        SelectedEntity.Effects.Remove(effect);
        _ = RebuildSprites();
    }

    // a new cell always starts at 0,0 (the middle of the target), where nearly every cell goes. Drag it to move it
    private async Task PlaceCell()
    {
        if (SelectedEntity is null) return;
        if (SelectedAnimationImageFilesIndex < 0 || SelectedAnimationImageFilesIndex >= AnimationImageFiles.Length) return;

        var file = AnimationImageFiles[SelectedAnimationImageFilesIndex];
        var frames = await graphicsService.GetAnimationFrames(file.FileName);
        if (frames < 1) frames = 1;

        var cell = NextCell();

        for (var i = 0; i < frames; i++)
        {
            SelectedEntity.Effects.Add(new AnimationEffect
            {
                Name = file.FileName,
                Frame = SelectedFrame + i,
                Cell = cell,
                Index = i,
                X = 0,
                Y = 0
            });
        }

        await RebuildSprites();
    }

    private int NextCell()
    {
        if (SelectedEntity is null || SelectedEntity.Effects.Count == 0) return 1;
        return SelectedEntity.Effects.Max(x => x.Cell) + 1;
    }

    private AnimationEffect? HitTest(Point position)
    {
        if (SelectedEntity is null) return null;

        // Iterate top-most (last drawn) first
        for (var i = Sprites.Count - 1; i >= 0; i--)
        {
            var sprite = Sprites[i];
            if (ReferenceEquals(sprite, _actorSprite) || sprite.Viewport is not { } viewport) continue;

            var width = viewport.Width * sprite.ScaleX;
            var height = viewport.Height * sprite.ScaleY;
            var centerX = sprite.X + viewport.Width / 2.0;
            var centerY = sprite.Y + viewport.Height / 2.0;
            var left = centerX - width / 2.0;
            var top = centerY - height / 2.0;

            if (position.X < left || position.X > left + width) continue;
            if (position.Y < top || position.Y > top + height) continue;

            return SelectedEntity.Effects
                .FirstOrDefault(x => x.Frame == SelectedFrame && x.Cell == sprite.Z);
        }

        return null;
    }

    private async Task RebuildSprites()
    {
        if (SelectedEntity is null)
        {
            Sprites.Clear();
            if (_actorSprite is not null) Sprites.Add(_actorSprite);
            return;
        }

        var effects = SelectedEntity.Effects
            .Where(x => x.Frame == SelectedFrame)
            .OrderBy(x => x.Cell)
            .ToList();

        // Build the full sprite list first (all async work done) so the canvas
        // never renders a half-cleared collection mid-rebuild (prevents flicker)
        // the actor stands under every cell, shockwaves and heat bend it but not the cells
        var rebuilt = new List<PictureBox>();
        if (_actorSprite is not null) rebuilt.Add(_actorSprite);
        rebuilt.AddRange(FrameDistortions());

        foreach (var effect in effects)
        {
            if (string.IsNullOrEmpty(effect.Name)) continue;

            var path = Path.Combine(graphicsService.GetAnimationImagesPath(), effect.Name);
            var viewport = await graphicsService.GetAnimationViewport(effect.Name, effect.Index);

            var (spriteX, spriteY) = EffectToSpritePosition(effect, viewport);

            rebuilt.Add(new PictureBox
            {
                ImagePath = path,
                Viewport = viewport,
                X = spriteX,
                Y = spriteY,
                Z = effect.Cell,
                Opacity = (float) effect.Opacity,
                ScaleX = (float) effect.ScaleX,
                ScaleY = (float) effect.ScaleY,
                Rotate = effect.Rotate,
                Hue = (float) effect.Hue,
                Contrast = (float) effect.Contrast,
                Saturation = (float) effect.Saturation,
                Brightness = (float) effect.Brightness,
                BlendMode = effect.BlendMode
            });
        }

        // Swap the contents in one tight synchronous batch
        Sprites.Clear();
        foreach (var sprite in rebuilt) Sprites.Add(sprite);

        UpdateCellOutlines();
        UpdateSelectionRect();
    }

    private void UpdateCellOutlines()
    {
        CellOutlines.Clear();

        foreach (var sprite in Sprites)
        {
            if (ReferenceEquals(sprite, _actorSprite) || sprite.Viewport is not { } viewport) continue;

            var width = viewport.Width * sprite.ScaleX;
            var height = viewport.Height * sprite.ScaleY;
            var centerX = sprite.X + viewport.Width / 2.0;
            var centerY = sprite.Y + viewport.Height / 2.0;
            CellOutlines.Add(new CellOutline(centerX - width / 2.0, centerY - height / 2.0, width, height));
        }
    }

    #endregion
}
