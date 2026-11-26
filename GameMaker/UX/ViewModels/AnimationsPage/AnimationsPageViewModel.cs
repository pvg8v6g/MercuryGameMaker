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

    // a map's ground picture behind everything, so shockwaves and heat have something to bend (none = plain canvas)
    private PictureBox? _backdropSprite;
    private const string NoBackdrop = "None";
    private const int BackdropZ = -2;

    // the room a cell's id takes in its outline's corner
    private const double LabelWidth = 24;

    // the frame's tint being edited, watched so the actor shows each change
    private AnimationTint? _watchedTint;

    // the frame's shockwave and heat being edited, watched so the preview shows each change
    private AnimationShockwave? _watchedShockwave;
    private AnimationHeatHaze? _watchedHeatHaze;
    private AnimationLightning? _watchedLightning;

    // shockwaves, heat and lightning are drawn over the actor and under the cells, like the game
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
            RefreshFrameSound();
            _ = RebuildSprites();
        }
    } = 1;

    // the files in the SFX folder, for the frame's sound
    public string[] SfxFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    // the animation plays a sound on this frame (an animation can be only sounds)
    public bool PlaysSound
    {
        get => FrameSound is not null;
        set
        {
            if (SelectedEntity is null || value == PlaysSound) return;

            if (value) SelectedEntity.Sounds.Add(new AnimationSound { Frame = SelectedFrame });
            else SelectedEntity.Sounds.Remove(FrameSound!);
            RefreshFrameSound();
        }
    }

    public AnimationSound? FrameSound => SelectedEntity?.Sounds.FirstOrDefault(x => x.Frame == SelectedFrame);

    // the combo box picks nothing while its list of files reloads (every visit to the page), which must not clear the sound
    public string? FrameSoundName
    {
        get => FrameSound?.Name;
        set
        {
            if (value is null || FrameSound is not { } sound || sound.Name == value) return;

            sound.Name = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand PreviewFrameSoundCommand => new(() =>
    {
        if (FrameSound is { Name: { Length: > 0 } name } sound) audioService.PlaySfx(name, sound.Volume, sound.Pitch);
    });

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

    // a bolt of lightning starts on this frame
    public bool StartsLightning
    {
        get => FrameLightning is not null;
        set
        {
            if (SelectedEntity is null || value == StartsLightning) return;

            if (value) SelectedEntity.Lightnings.Add(new AnimationLightning { Frame = SelectedFrame });
            else SelectedEntity.Lightnings.Remove(FrameLightning!);
            RefreshFrameDistortions();
            _ = RebuildSprites();
        }
    }

    public AnimationLightning? FrameLightning => SelectedEntity?.Lightnings.FirstOrDefault(x => x.Frame == SelectedFrame);

    // the lightning's colour for the colour picker (saved as #RRGGBB)
    public Windows.UI.Color LightningColor
    {
        get => FrameLightning?.Color.ToColorOr(Colors.White) ?? Colors.White;
        set
        {
            if (FrameLightning is not { } lightning || lightning.Color == value.ToHex()) return;

            lightning.Color = value.ToHex();
            OnPropertyChanged();
        }
    }

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    // the map ground pictures that can sit behind the preview, with None first
    public IReadOnlyList<string> BackdropFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public string? SelectedBackdrop
    {
        get;
        set
        {
            if (SetField(ref field, value)) _ = LoadBackdrop();
        }
    }

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
            OnPropertyChanged(nameof(PlaySymbol));
        }
    }

    public Symbol PlaySymbol => IsPlaying ? Symbol.Stop : Symbol.Play;

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        RefreshAnimationImageFiles();
        SfxFiles = mediaService.GetMediaFiles(Cache.Sfx);
        RefreshFrameSound();
        await LoadActorSprite();
        UpdateAnchor();
        RefreshBackdropFiles();
        await LoadBackdrop();
    }

    protected override async Task OnSelectedIndexChanged(int index)
    {
        StopPlayback();
        SelectedCell = null;
        SelectedFrame = 1;
        RefreshFrameTint();
        RefreshFrameDistortions();
        RefreshFrameSound();
        await RebuildSprites();
    }

    #endregion

    private void RefreshFrameSound()
    {
        OnPropertyChanged(nameof(PlaysSound));
        OnPropertyChanged(nameof(FrameSound));
        OnPropertyChanged(nameof(FrameSoundName));
    }

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

        if (_watchedLightning is not null) _watchedLightning.PropertyChanged -= OnFrameDistortionChanged;
        _watchedLightning = FrameLightning;
        if (_watchedLightning is not null) _watchedLightning.PropertyChanged += OnFrameDistortionChanged;

        OnPropertyChanged(nameof(StartsShockwave));
        OnPropertyChanged(nameof(FrameShockwave));
        OnPropertyChanged(nameof(StartsHeatHaze));
        OnPropertyChanged(nameof(FrameHeatHaze));
        OnPropertyChanged(nameof(StartsLightning));
        OnPropertyChanged(nameof(FrameLightning));
        OnPropertyChanged(nameof(LightningColor));
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

        // the same bolt every time a frame is shown, so stepping through frames shows how it crackles
        foreach (var lightning in animation.Lightnings.Where(x => frame >= x.Frame && frame <= x.LastFrame))
        {
            var bolt = new Lightning
            {
                Z = DistortionZ,
                X = (int) (centerX + lightning.StartX),
                Y = (int) (centerY + lightning.StartY),
                ToX = (float) (lightning.EndX - lightning.StartX),
                ToY = (float) (lightning.EndY - lightning.StartY)
            };
            lightning.ApplyTo(bolt, frame - lightning.Frame, 1.0, lightning.Frame);
            yield return bolt;
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

        // the last frame with a cell, a tint, a sound, or a shockwave or heat still going, the way the game plays it
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

        foreach (var sound in SelectedEntity.Sounds.Where(x => x.Frame == frame))
        {
            if (!string.IsNullOrEmpty(sound.Name)) audioService.PlaySfx(sound.Name, sound.Volume, sound.Pitch);
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

    // the map ground pictures (Graphics\Maps\Ground), the first one picked when nothing is chosen yet
    private void RefreshBackdropFiles()
    {
        var folder = graphicsService.GetMapGroundPath();
        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.png").Select(Path.GetFileName).OfType<string>().Order(StringComparer.OrdinalIgnoreCase)
            : Enumerable.Empty<string>();
        BackdropFiles = [NoBackdrop, .. files];

        if (SelectedBackdrop is null || !BackdropFiles.Contains(SelectedBackdrop))
        {
            SelectedBackdrop = BackdropFiles.Count > 1 ? BackdropFiles[1] : NoBackdrop;
        }
    }

    private async Task LoadBackdrop()
    {
        _backdropSprite = null;
        if (SelectedBackdrop is { } name && name != NoBackdrop)
        {
            var path = Path.Combine(graphicsService.GetMapGroundPath(), name);
            if (File.Exists(path))
            {
                var (width, height) = await graphicsService.GetImageDimensions(path);
                _backdropSprite = new PictureBox { ImagePath = path, Viewport = new Rect(0, 0, width, height), Z = BackdropZ };
            }
        }

        UpdateAnchor();
        await RebuildSprites();
    }

    private void UpdateAnchor()
    {
        AnchorX = (int) ((CanvasWidth - AnchorWidth) / 2.0);
        AnchorY = (int) ((CanvasHeight - AnchorHeight) / 2.0);

        // the backdrop's middle sits under the middle of the actor
        if (_backdropSprite?.Viewport is { } backdrop)
        {
            _backdropSprite.X = (int) ((CanvasWidth - backdrop.Width) / 2.0);
            _backdropSprite.Y = (int) ((CanvasHeight - backdrop.Height) / 2.0);
        }

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

        var popupViewModel = new AnimationEffectsPopupViewModel(SelectedCell, graphicsService);
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
                BlendMode = x.BlendMode
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
            if (ReferenceEquals(sprite, _actorSprite) || ReferenceEquals(sprite, _backdropSprite) || sprite.Viewport is not { } viewport) continue;

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
            if (_backdropSprite is not null) Sprites.Add(_backdropSprite);
            if (_actorSprite is not null) Sprites.Add(_actorSprite);
            UpdateCellOutlines();
            return;
        }

        var effects = SelectedEntity.Effects
            .Where(x => x.Frame == SelectedFrame)
            .OrderBy(x => x.Cell)
            .ToList();

        // Build the full sprite list first (all async work done) so the canvas
        // never renders a half-cleared collection mid-rebuild (prevents flicker)
        // the actor stands under every cell, shockwaves and heat bend it but not the cells
        // the backdrop goes first: the canvas draws in list order and a distortion bends everything before it
        var rebuilt = new List<PictureBox>();
        if (_backdropSprite is not null) rebuilt.Add(_backdropSprite);
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
            if (ReferenceEquals(sprite, _actorSprite) || ReferenceEquals(sprite, _backdropSprite) || sprite.Viewport is not { } viewport) continue;

            var width = viewport.Width * sprite.ScaleX;
            var height = viewport.Height * sprite.ScaleY;
            var centerX = sprite.X + viewport.Width / 2.0;
            var centerY = sprite.Y + viewport.Height / 2.0;
            var x = centerX - width / 2.0;
            var y = centerY - height / 2.0;

            // cells on top of each other put their ids side by side
            var taken = CellOutlines.Count(o => Math.Abs(o.X + o.Width - x - width) < LabelWidth && Math.Abs(o.Y - y) < LabelWidth);
            CellOutlines.Add(new CellOutline(x, y, width, height, sprite.Z.ToString(), -taken * LabelWidth));
        }
    }

    #endregion
}
