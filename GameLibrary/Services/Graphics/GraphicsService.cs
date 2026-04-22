using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using GameLibrary.Enumerations;
using GameLibrary.Services.Location;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.Extensions;
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
        return new CroppedImage { ImagePath = path, ImageSource = source, Rect = viewport };
    }

    public async Task<CroppedImage> GetEngineIcon(int index)
    {
        var path = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png");
        var source = await GetImage(path);
        var dimensions = await GetImageDimensions(path);
        var w = dimensions.width / 48.0d;
        var viewport = new Rect(((int) (index % w)) * 48.0, ((int) (index / w)) * 48.0, 48.0, 48.0);
        return new CroppedImage { ImagePath = path, ImageSource = source, Rect = viewport };
    }

    #endregion

    #region Character

    public string GetCharacterPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Graphics", "Characters");
    }

    public Direction GetCharacterDirectionFromIndex(string fileName, int index)
    {
        var divisions = GetCharacterDivisions(fileName);
        return Enum.GetValues<Direction>().FirstOrDefault(x => (int) (index / divisions.x) == (int) x);
    }

    public int GetCharacterIndexFromDirection(string fileName, int index, Direction direction)
    {
        var divisions = GetCharacterDivisions(fileName);
        var t0 = (int) direction * (int) divisions.x + index;
        return (int) direction * (int) divisions.x + index;
    }

    public async Task<CroppedImage> GetCharacter(string fileName, int index, Direction direction)
    {
        var path = Path.Combine(GetCharacterPath(), fileName);
        var source = await GetImage(path);
        var dimensions = await GetImageDimensions(path);
        var divisions = GetCharacterDivisions(fileName);
        var w = dimensions.width / divisions.x;
        var h = dimensions.height / divisions.y;
        var bitIndex = (int) direction * divisions.x + index;
        var x = ((int) (bitIndex % divisions.x)) * w;
        var y = ((int) (bitIndex / divisions.x)) * h;
        return new CroppedImage { ImagePath = path, ImageSource = source, Rect = new Rect(x, y, w, h) };
    }

    #endregion

    #region Face

    public void GetFace(string fileName)
    {
    }

    #endregion

    #region Animation

    public string GetAnimationImagesPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Graphics", "Animations");
    }

    public FileSelection[] GetAnimationImages()
    {
        var path = GetAnimationImagesPath();
        var files = Directory.GetFiles(path, "*.png");
        return files.Select(x => new FileSelection(Path.GetFileName(x), x)).ToArray();
    }

    #endregion

    #region Map

    #endregion

    #region Images

    public async Task<(double width, double height)> GetImageDimensions(string imagePath)
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
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));
        var name = Path.GetFileName(fileName);
        return name[0] switch
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
