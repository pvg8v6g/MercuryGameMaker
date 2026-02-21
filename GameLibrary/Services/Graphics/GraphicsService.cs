using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using GameLibrary.Services.Location;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Services.Graphics;

public class GraphicsService(ILocationService locationService) : IGraphicsService
{
    #region Icon

    public string GetIconsPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Graphics", "Icons", "~Icons.png");
    }

    public async Task<CroppedImage> GetIcon(int index)
    {
        var path = GetIconsPath();
        var source = await GetImage(path);
        var dimensions = await GetImageDimensions(path);
        var (w, h) = await GetSegmentation(path);
        var columns = dimensions.width / w;
        var x = (index % columns) * w;
        var y = ((int) (index / columns)) * w;
        var viewport = new Rect(x, y, w, h);
        return new CroppedImage { ImageSource = source, Rect = viewport };
    }

    public async Task<CroppedImage> GetEngineIcon(int index)
    {
        var path = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png");
        var source = await GetImage(path);
        var dimensions = await GetImageDimensions(path);
        var w = dimensions.width / 48.0d;
        var viewport = new Rect(((int) (index % w)) * 48.0, ((int) (index / w)) * 48.0, 48.0, 48.0);
        return new CroppedImage { ImageSource = source, Rect = viewport };
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

    private async Task<(double width, double height)> GetImageDimensions(string imagePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);

        return (decoder.PixelWidth, decoder.PixelHeight);
    }

    public async Task<BitmapImage> GetImage(string imagePath)
    {
        var path = Path.IsPathRooted(imagePath) ? imagePath : Path.Combine(locationService.GraphicsDirectory!, imagePath);

        if (ImagesCache.Count > 200) ImagesCache.Clear(); // Clear cache if it gets too big
        if (ImagesCache.TryGetValue(path, out var cachedImage))
        {
            return cachedImage;
        }

        var bitmapImage = new BitmapImage(new Uri(path, UriKind.Absolute));
        ImagesCache[path] = bitmapImage;
        return await Task.FromResult(bitmapImage);
    }

    public async Task<(double width, double height)> GetSegmentation(string fileName)
    {
        if (Path.GetFileName(fileName).StartsWith('~')) return (32.0d, 32.0d);
        var dimensions = await GetImageDimensions(fileName);
        var divisions = GetCharacterDivisions(fileName);
        return (dimensions.width / divisions.x, dimensions.height / divisions.y);
    }

    public (double x, double y) GetCharacterDivisions(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
        return fileName[0] switch
        {
            '$' => (3.0d, 4.0d),
            '@' => (4.0d, 2.0d),
            '#' => (16.0d, 32.0d),
            '&' => (4.0d, 2.0d),
            _ => (12.0d, 8.0d),
        };
    }

    #endregion

    #region Cache

    private Dictionary<string, BitmapImage> ImagesCache { get; } = new();

    private Dictionary<string, BitmapImage> AnimationsCache { get; } = new();

    #endregion
}
