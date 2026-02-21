using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.Components.IconView;

public partial class IconView
{
    public static readonly DependencyProperty IconIndexProperty = DependencyProperty.Register(
        nameof(IconIndex), typeof(int), typeof(IconView), new PropertyMetadata(0, OnIconIndexChanged));

    private static async void OnIconIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not IconView iconView) return;
        iconView.UpdateSelectedBorder();
        await iconView.UpdateCroppedImage();
    }

    public int IconIndex
    {
        get => (int) GetValue(IconIndexProperty);
        set => SetValue(IconIndexProperty, value);
    }

    public CroppedImage CroppedImage { get; } = new();

    public IconView()
    {
        InitializeComponent();
        IconFlyout.Opened += IconFlyout_Opened;
        Loaded += async (_, _) => await UpdateCroppedImage();
    }

    private async Task UpdateCroppedImage()
    {
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var icon = await graphicsService.GetIcon(IconIndex);
        CroppedImage.ImageSource = icon.ImageSource;
        CroppedImage.Rect = icon.Rect;
        UpdateSelectedBorder();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        UpdateSelectedBorder();
        FlyoutBase.ShowAttachedFlyout(RootGrid);
    }

    private async void IconFlyout_Opened(object? sender, object e)
    {
        if (FullIconsImage.Source == null)
        {
            var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
            var path = graphicsService.GetIconsPath();
            var bitmapImage = await graphicsService.GetImage(path);

            if (bitmapImage.PixelWidth == 0)
            {
                bitmapImage.ImageOpened += (_, _) =>
                {
                    UpdateSelectedBorder();
                    ScrollToSelected();
                };
            }

            FullIconsImage.Source = bitmapImage;
        }

        UpdateSelectedBorder();
        ScrollToSelected();
    }

    private void ScrollToSelected()
    {
        if (CroppedImage.Rect == null) return;

        var y = CroppedImage.Rect.Value.Y;

        // Ensure the layout is updated so ViewportWidth and ViewportHeight are correct
        IconScrollViewer.UpdateLayout();

        // Center the 32x32 icon in the view if possible
        var offsetY = y - (IconScrollViewer.ViewportHeight / 2) + (CroppedImage.Rect.Value.Y / 2);

        IconScrollViewer.ChangeView(0, offsetY, null);
    }

    private void UpdateSelectedBorder()
    {
        if (FullIconsImage.Source is BitmapImage bitmapImage)
        {
            if (bitmapImage.PixelWidth == 0 || CroppedImage.Rect == null) return;

            var x = (int) CroppedImage.Rect.Value.X;
            var y = (int) CroppedImage.Rect.Value.Y;

            Canvas.SetLeft(SelectedBorder, x);
            Canvas.SetTop(SelectedBorder, y);
            SelectedBorder.Visibility = Visibility.Visible;

            OverlayCanvas.Width = bitmapImage.PixelWidth;
            OverlayCanvas.Height = bitmapImage.PixelHeight;
            IconsContainer.Width = bitmapImage.PixelWidth;
            IconsContainer.Height = bitmapImage.PixelHeight;
            FullIconsImage.Width = bitmapImage.PixelWidth;
            FullIconsImage.Height = bitmapImage.PixelHeight;
        }
    }

    private void IconsContainer_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var (w, h) = graphicsService.GetSegmentation("~Icons.png").Result;
        var position = e.GetCurrentPoint(IconsContainer).Position;

        var x = (int) (position.X / w) * w;
        var y = (int) (position.Y / h) * h;

        Canvas.SetLeft(HoverBorder, x);
        Canvas.SetTop(HoverBorder, y);
        HoverBorder.Visibility = Visibility.Visible;
    }

    private void IconsContainer_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverBorder.Visibility = Visibility.Collapsed;
    }

    private void FullIconsImage_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var (w, h) = graphicsService.GetSegmentation("~Icons.png").Result;
        var position = e.GetPosition(IconsContainer);
        var xIndex = (int) (position.X / w);
        var yIndex = (int) (position.Y / h);

        if (FullIconsImage.Source is not BitmapImage bitmapImage) return;
        var columns = (int) (bitmapImage.PixelWidth / w);
        var index = yIndex * columns + xIndex;
        IconIndex = index;
        IconFlyout.Hide();
    }
}
