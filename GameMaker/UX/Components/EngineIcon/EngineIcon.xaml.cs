using Microsoft.UI.Xaml;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Components.EngineIcon;

public sealed partial class EngineIcon
{
    public static readonly DependencyProperty IconIndexProperty = DependencyProperty.Register(
        nameof(IconIndex), typeof(int), typeof(EngineIcon), new PropertyMetadata(0, OnIconIndexChanged));

    private static async void OnIconIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EngineIcon engineIcon) return;
        await engineIcon.UpdateCroppedImage();
    }

    public int IconIndex
    {
        get => (int) GetValue(IconIndexProperty);
        set => SetValue(IconIndexProperty, value);
    }

    public CroppedImage CroppedImage { get; } = new();

    public EngineIcon()
    {
        InitializeComponent();
        Loaded += async (_, _) => await UpdateCroppedImage();
    }

    private async Task UpdateCroppedImage()
    {
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var icon = await graphicsService.GetIcon(IconIndex);
        CroppedImage.ImageSource = icon.ImageSource;
        CroppedImage.Rect = icon.Rect;
    }
}
