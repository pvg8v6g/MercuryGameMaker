using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Media;
using GameLibrary.Utilities.ComponentModels;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameMaker.Services.Navigation;
using GameMaker.UX.Models.AnimationsPage;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using MercuryLibrary.ImageComponents;
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
            _ = RebuildSprites();
        }
    } = 1;

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

    private int AnchorSize
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
        AnchorSize = gameDataService.GridSize;
        await RebuildSprites();
    }

    protected override async Task OnSelectedIndexChanged(int index)
    {
        StopPlayback();
        SelectedCell = null;
        SelectedFrame = 1;
        await RebuildSprites();
    }

    #endregion

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
        if (SelectedEntity is null || SelectedEntity.Effects.Count == 0) return;

        _lastFrame = SelectedEntity.Effects.Max(x => x.Frame);
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

    private void UpdateAnchor()
    {
        AnchorX = (int) ((CanvasWidth - AnchorSize) / 2.0);
        AnchorY = (int) ((CanvasHeight - AnchorSize) / 2.0);
        AnchorRect = new Rect(AnchorX, AnchorY, AnchorSize, AnchorSize);
    }

    private (int X, int Y) EffectToSpritePosition(AnimationEffect effect, Rect? viewport)
    {
        // 0,0 on the animation form maps to the center of the anchor, aligned with
        // the center of the sprite's viewport (not its top-left corner).
        var centerX = AnchorX + AnchorSize / 2.0 + effect.X;
        var centerY = AnchorY + AnchorSize / 2.0 + effect.Y;
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

        if (info.Properties.IsLeftButtonPressed) _ = PlaceCell(position);
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

    private async Task PlaceCell(Point position)
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
            if (sprite.Viewport is not { } viewport) continue;

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
            return;
        }

        var effects = SelectedEntity.Effects
            .Where(x => x.Frame == SelectedFrame)
            .OrderBy(x => x.Cell)
            .ToList();

        // Build the full sprite list first (all async work done) so the canvas
        // never renders a half-cleared collection mid-rebuild (prevents flicker)
        var rebuilt = new List<PictureBox>();

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
            if (sprite.Viewport is not { } viewport) continue;

            var width = viewport.Width * sprite.ScaleX;
            var height = viewport.Height * sprite.ScaleY;
            var centerX = sprite.X + viewport.Width / 2.0;
            var centerY = sprite.Y + viewport.Height / 2.0;
            CellOutlines.Add(new CellOutline(centerX - width / 2.0, centerY - height / 2.0, width, height));
        }
    }

    #endregion
}
