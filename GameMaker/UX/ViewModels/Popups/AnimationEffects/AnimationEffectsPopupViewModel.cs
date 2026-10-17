using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Media;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Media;
using MercuryLibrary.Enumerations;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.AnimationEffects;

public class AnimationEffectsPopupViewModel : PropertyChangedUpdater
{
    #region Fields

    private readonly IGraphicsService _graphicsService;
    private readonly IMediaService _mediaService;
    private readonly IAudioService _audioService;
    private PictureBox? _preview;

    #endregion

    public AnimationEffectsPopupViewModel(
        AnimationEffect effect,
        IGraphicsService graphicsService,
        IMediaService mediaService,
        IAudioService audioService)
    {
        _graphicsService = graphicsService;
        _mediaService = mediaService;
        _audioService = audioService;
        Effect = effect;
        effect.PropertyChanged += OnEffectChanged;

        SfxFiles = ["(None)", .. _mediaService.GetMediaFiles(Cache.Sfx)];
        SelectedSfx = effect.Sfx?.Name ?? "(None)";

        _ = BuildPreview();
    }

    #region Properties

    public AnimationEffect Effect { get; }

    public double RotateValue
    {
        get => Effect.Rotate;
        set
        {
            if ((int) value == Effect.Rotate) return;
            Effect.Rotate = (int) value;
            OnPropertyChanged();
        }
    }

    public BlendMode[] BlendModes { get; } = Enum.GetValues<BlendMode>();

    public string[] SfxFiles { get; }

    public string SelectedSfx
    {
        get;
        set
        {
            var normalized = string.IsNullOrEmpty(value) ? "(None)" : value;
            if (normalized == field) return;
            field = normalized;

            if (normalized == "(None)")
            {
                Effect.Sfx = null;
            }
            else
            {
                Effect.Sfx ??= new AudioFile { Path = "SFX" };
                Effect.Sfx.Name = normalized;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSfxSelected));
            OnPropertyChanged(nameof(SfxVolume));
            OnPropertyChanged(nameof(SfxPitch));
            PreviewSfxCommand.RaiseCanExecuteChanged();
        }
    } = "(None)";

    public bool IsSfxSelected => Effect.Sfx is not null;

    public double SfxVolume
    {
        get => Effect.Sfx?.Volume ?? 1.0;
        set
        {
            if (Effect.Sfx is null) return;
            Effect.Sfx.Volume = value;
            OnPropertyChanged();
        }
    }

    public double SfxPitch
    {
        get => Effect.Sfx?.Pitch ?? 1.0;
        set
        {
            if (Effect.Sfx is null) return;
            Effect.Sfx.Pitch = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand PreviewSfxCommand => field ??= new RelayCommand(PreviewSfx, () => IsSfxSelected);

    public double PreviewSize => 256.0;

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    #endregion

    #region Private methods

    private void PreviewSfx()
    {
        if (Effect.Sfx?.Name is not { } name || string.IsNullOrEmpty(name)) return;
        _audioService.PlaySfx(name, Effect.Sfx.Volume, Effect.Sfx.Pitch);
    }

    private void OnEffectChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyEffect();
    }

    private async Task BuildPreview()
    {
        Sprites.Clear();

        if (string.IsNullOrEmpty(Effect.Name)) return;

        var path = Path.Combine(_graphicsService.GetAnimationImagesPath(), Effect.Name);
        var viewport = await _graphicsService.GetAnimationViewport(Effect.Name, Effect.Index);

        _preview = new PictureBox
        {
            ImagePath = path,
            Viewport = viewport
        };

        ApplyEffect();
        Sprites.Add(_preview);
    }

    private void ApplyEffect()
    {
        if (_preview?.Viewport is not { } viewport) return;

        _preview.X = (int) ((PreviewSize - viewport.Width) / 2.0);
        _preview.Y = (int) ((PreviewSize - viewport.Height) / 2.0);
        _preview.Opacity = (float) Effect.Opacity;
        _preview.ScaleX = (float) Effect.ScaleX;
        _preview.ScaleY = (float) Effect.ScaleY;
        _preview.Rotate = Effect.Rotate;
        _preview.Hue = (float) Effect.Hue;
        _preview.Contrast = (float) Effect.Contrast;
        _preview.Saturation = (float) Effect.Saturation;
        _preview.Brightness = (float) Effect.Brightness;
        _preview.BlendMode = Effect.BlendMode;
    }

    #endregion
}
