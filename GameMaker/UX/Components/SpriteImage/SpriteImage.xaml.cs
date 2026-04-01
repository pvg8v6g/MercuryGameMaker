using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.SpriteImage;

public partial class SpriteImage
{
    #region Registered Dependencies

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(nameof(CharacterImage), typeof(CroppedImage),
        typeof(SpriteImage), new PropertyMetadata(null));

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    #endregion

    public SpriteImage()
    {
        InitializeComponent();
    }
}
