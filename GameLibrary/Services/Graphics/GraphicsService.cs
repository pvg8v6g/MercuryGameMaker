using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using GameLibrary.Services.Location;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;

namespace GameLibrary.Services.Graphics;

public class GraphicsService : IGraphicsService
{
    #region Fields

    private readonly ILocationService locationService;
    private readonly CanvasDevice? _canvasDevice;
    private CanvasBitmap? _iconSpriteSheet;

    #endregion

    #region Constructor

    public GraphicsService(ILocationService locationService)
    {
        this.locationService = locationService;
        _canvasDevice = CanvasDevice.GetSharedDevice();
    }

    #endregion

    #region Icon

    // public static Image getEngineIcon() {
    //     if (engineIcons == null) engineIcons = new Image(LocationService.engineLocation + "/Icons/Icons.png");
    //     return engineIcons;
    // }
    //
    // public static Rectangle2D getEngineIconViewport(Image image, int index) {
    //     var w = image.getWidth() / 48.0;
    //     return new Rectangle2D(((int) (index % w)) * 48.0, ((int) (index / w)) * 48.0, 48.0, 48.0);
    // }

    // public async Task<Image> GetEngineIcon(int index)
    // {
    //     var imagePath = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png");
    //     var dimensions = await GetImageDimensions(imagePath);
    //
    //     var width = dimensions.width / 48.0;
    //     var x = ((int) (index % width)) * 48.0;
    //     var y = ((int) (index / width)) * 48.0;
    //
    //     var bitmapImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
    //     var image = new Image
    //     {
    //         Source = bitmapImage,
    //         Width = dimensions.width, // Full sprite sheet width
    //         Height = dimensions.height, // Full sprite sheet height
    //         // Margin = new Thickness(-x, -y, 0, 0),
    //         // HorizontalAlignment = HorizontalAlignment.Left,
    //         // VerticalAlignment = VerticalAlignment.Top,
    //         Stretch = Stretch.None
    //     };
    //
    //     // var grid = new Grid
    //     // {
    //     //     Width = 48,
    //     //     Height = 48,
    //     //     Clip = new RectangleGeometry
    //     //     {
    //     //         Rect = new Rect(0, 0, 48, 48)
    //     //     }
    //     // };
    //     //
    //     // grid.Children.Add(image);
    //
    //     return image; // var image = new Image
    //     // {
    //     //     Source = bitmapImage,
    //     //     Margin = new Thickness(-x, -y, 0, 0),
    //     //     HorizontalAlignment = HorizontalAlignment.Left,
    //     //     VerticalAlignment = VerticalAlignment.Top
    //     // };
    //     //
    //     // var grid = new Grid
    //     // {
    //     //     Width = 48,
    //     //     Height = 48,
    //     //     Children = { image }
    //     // };
    //     //
    //     // return grid;
    // }

    public async Task<Canvas> GetEngineIconImproved(int index)
    {
        var imagePath = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png");
        var dimensions = await GetImageDimensions(imagePath);

        var width = dimensions.width / 48.0;
        var x = ((int) (index % width)) * 48.0;
        var y = ((int) (index / width)) * 48.0;

        var bitmapImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        var image = new Image { Source = bitmapImage };

        var clip = new RectangleGeometry { Rect = new Rect(x, y, 48, 48) };

        return new Canvas { Width = 48, Height = 48, Clip = clip, Children = { image } };
    }

    public async Task<CanvasImageSource> GetEngineIcon(int index)
    {
        // Load sprite sheet once and cache it
        if (_iconSpriteSheet == null)
        {
            var imagePath = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png");
            _iconSpriteSheet = await CanvasBitmap.LoadAsync(_canvasDevice, imagePath);
        }

        // Calculate sprite position
        var width = _iconSpriteSheet.SizeInPixels.Width / 48.0;
        var x = ((int) (index % width)) * 48;
        var y = ((int) (index / width)) * 48;

        // Create a 48x48 image source
        var imageSource = new CanvasImageSource(_canvasDevice, 48, 48, 96);

        // Draw the specific sprite
        using var ds = imageSource.CreateDrawingSession(Colors.Transparent);
        ds.DrawImage(
            _iconSpriteSheet,
            new Rect(0, 0, 48, 48),
            new Rect(x, y, 48, 48),
            1.0f,
            CanvasImageInterpolation.NearestNeighbor
        );

        return imageSource;
    }

    #endregion

    #region Character

    #endregion

    #region Face

    #endregion

    #region Animation

    #endregion

    #region Map

    #endregion

    #region Images

    private async Task<(uint width, uint height)> GetImageDimensions(string imagePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);

        return (decoder.PixelWidth, decoder.PixelHeight);
    }

    public async Task<BitmapImage> GetImage(string imagePath)
    {
        var path = Path.Combine(locationService.GraphicsDirectory!, imagePath);

        if (ImagesCache.Count > 200) ImagesCache.Clear(); // Clear cache if it gets too big
        if (ImagesCache.TryGetValue(imagePath, out var cachedImage))
        {
            return cachedImage;
        }

        var bitmapImage = new BitmapImage(new Uri(path, UriKind.Absolute));
        ImagesCache[imagePath] = bitmapImage;
        return await Task.FromResult(bitmapImage);
    }

    #endregion

    #region Cache

    public Dictionary<string, BitmapImage> ImagesCache { get; } = new();

    public Dictionary<string, BitmapImage> AnimationsCache { get; } = new();

    #endregion
}
