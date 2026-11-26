using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using GameLibrary.Models.Animations;
using GameLibrary.Services.Graphics;
using MercuryLibrary.Enumerations;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.AnimationEffects;

public class AnimationEffectsPopupViewModel : PropertyChangedUpdater
{
    #region Fields

    private readonly IGraphicsService _graphicsService;
    private PictureBox? _preview;

    #endregion

    public AnimationEffectsPopupViewModel(
        AnimationEffect effect,
        IGraphicsService graphicsService)
    {
        _graphicsService = graphicsService;
        Effect = effect;
        effect.PropertyChanged += OnEffectChanged;

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

    public double PreviewSize => 256.0;

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    #endregion

    #region Private methods

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
