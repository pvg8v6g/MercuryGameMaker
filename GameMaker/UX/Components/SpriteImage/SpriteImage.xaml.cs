using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Components.SpriteImage;

public partial class SpriteImage
{
    #region Registered Dependencies

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(nameof(CharacterImage), typeof(CroppedImage),
        typeof(SpriteImage), new PropertyMetadata(null, OnCharacterImageChanged));

    private static void OnCharacterImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpriteImage spriteImage)
        {
            spriteImage.Bindings.Update();
        }
    }

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    public static readonly DependencyProperty SpriteBackgroundProperty = DependencyProperty.Register(nameof(SpriteBackground), typeof(Brush),
        typeof(SpriteImage), new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.Transparent)));

    public Brush SpriteBackground
    {
        get => (Brush) GetValue(SpriteBackgroundProperty);
        set => SetValue(SpriteBackgroundProperty, value);
    }

    #endregion

    public SpriteImage()
    {
        InitializeComponent();
    }
}
