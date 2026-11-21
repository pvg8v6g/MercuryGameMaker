using GameLibrary.Models;
using GameLibrary.Utilities;
using Microsoft.UI.Xaml;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Components.EngineIcon;

public sealed partial class EngineIcon
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(GameIcon), typeof(EngineIcon), new PropertyMetadata(GameIcon.None, OnIconChanged));

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EngineIcon engineIcon) engineIcon.UpdateCroppedImage().Forget();
    }

    // the icon and the sheet it's on
    public GameIcon Icon
    {
        get => (GameIcon) GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public CroppedImage CroppedImage { get; } = new();

    public EngineIcon()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateCroppedImage().Forget();
    }

    private async Task UpdateCroppedImage()
    {
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var icon = await graphicsService.GetIcon(Icon ?? GameIcon.None);
        CroppedImage.ImageSource = icon.ImageSource;
        CroppedImage.Rect = icon.Rect;
    }
}
