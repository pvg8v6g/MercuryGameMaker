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

    #endregion

    #region Balloon

    public string GetBalloonsPath()
    {
        return Path.Combine(locationService.GameDirectory!, "Graphics", "Balloons", "~Balloons.png");
    }

    // a frame of a balloon: the row is the balloon, the column the frame. Empty when the sheet isn't there yet
    public async Task<CroppedImage> GetBalloonFrame(int row, int column)
    {
        var path = GetBalloonsPath();
        if (!File.Exists(path)) return new CroppedImage { ImagePath = path };

        var source = await GetImage(path);
        var size = Models.Balloons.BalloonIcon.CellSize;
        return new CroppedImage { ImagePath = path, ImageSource = source, Rect = new Rect(column * size, row * size, size, size) };
    }

    // how many frames (columns) and balloons (rows) the sheet has
    public async Task<(int columns, int rows)> GetBalloonDivisions()
    {
        var path = GetBalloonsPath();
        if (!File.Exists(path)) return (0, 0);

        var (width, height) = await GetImageDimensions(path);
        var size = Models.Balloons.BalloonIcon.CellSize;
        return ((int) (width / size), (int) (height / size));
    }

    #endregion

    #region Engine Icon

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
        return (int) direction * (int) divisions.x + index;
    }

    public async Task<CroppedImage> GetCharacter(string fileName, int index, Direction direction)
    {
        var path = Path.Combine(GetCharacterPath(), fileName);

        // missing file (renamed or deleted) - return an empty image so previews clear instead of throwing
        if (!File.Exists(path)) return new CroppedImage { ImagePath = path };

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
        return [.. files.Select(x => new FileSelection(Path.GetFileName(x), x))];
    }

    public async Task<CroppedImage> GetAnimationImage(string fileName, int frame)
    {
        var path = Path.Combine(GetAnimationImagesPath(), fileName);
        var source = await GetImage(path, Cache.Animation);
        var dimensions = await GetImageDimensions(path);
        var w = dimensions.width / 5.0;
        var h = dimensions.height / 5.0;
        var x = (frame % 5.0) * w;
        var y = (frame / 5.0) * h;
        return new CroppedImage { ImagePath = path, ImageSource = source, Rect = new Rect(x, y, w, h) };
    }

    public async Task<Rect> GetAnimationViewport(string fileName, int index)
    {
        var path = Path.Combine(GetAnimationImagesPath(), fileName);
        var dimensions = await GetImageDimensions(path);
        var size = dimensions.width / 5.0;
        var x = (index % 5) * size;
        var y = (index / 5) * size;
        return new Rect(x, y, size, size);
    }

    public async Task<int> GetAnimationFrames(string fileName)
    {
        var path = Path.Combine(GetAnimationImagesPath(), fileName);
        var dimensions = await GetImageDimensions(path);
        var vs = dimensions.width / 5.0;
        var h = (int) (dimensions.height / vs);
        return 5 * h;
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

    public async Task<BitmapImage> GetImage(string imagePath, Cache cache = Cache.Regular)
    {
        var path = Path.IsPathRooted(imagePath) ? imagePath : Path.Combine(locationService.GraphicsDirectory!, imagePath);

        var imageCache = cache switch
        {
            Cache.Regular => ImagesCache,
            Cache.Animation => AnimationsCache,
            _ => throw new ArgumentOutOfRangeException(nameof(cache), cache, null)
        };

        if (imageCache.Count > 200) imageCache.Clear(); // Clear cache if it gets too big
        if (imageCache.TryGetValue(path, out var cachedImage))
        {
            return cachedImage;
        }

        var bitmapImage = new BitmapImage(new Uri(path, UriKind.Absolute));
        imageCache[path] = bitmapImage;
        return await Task.FromResult(bitmapImage);
    }

    public async Task<(double width, double height)> GetSegmentation(string fileName)
    {
        if (Path.GetFileName(fileName).StartsWith('~')) return (32.0d, 32.0d);
        var dimensions = await GetImageDimensions(fileName);
        var divisions = GetCharacterDivisions(fileName);

        // face sheets are always 4 across but can have any number of rows, so the cells are square
        if (Path.GetFileName(fileName).StartsWith('&'))
        {
            var size = dimensions.width / divisions.x;
            return (size, size);
        }

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

    #region Props

    // the picture a placed prop is drawn with. The canvas can't mirror or fade a picture, so a flipped prop or one
    // buried in the ground uses a copy made once and cached (remade when the original changes). The copy keeps the
    // original size, a buried prop just has its bottom cut away and the line above it faded, so nothing moves.
    // The cache sits in a short folder on purpose, see PriPathGuard
    public async Task<string> GetPropImagePath(string imagePath, bool flip, int bury, int feather)
    {
        bury = Math.Max(0, bury);
        feather = Math.Max(0, feather);
        if (!flip && bury == 0) return imagePath;

        var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MercuryGameMaker", "cache", "props");
        Directory.CreateDirectory(cache);

        // the full path decides the name, two games can have a tree_oak.png that look nothing alike
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(imagePath).ToLowerInvariant())))[..12];
        var processed = Path.Combine(cache, $"{Path.GetFileNameWithoutExtension(imagePath)}_{hash}_{(flip ? "f" : "n")}_{bury}_{feather}.png");
        if (File.Exists(processed) && File.GetLastWriteTimeUtc(processed) >= File.GetLastWriteTimeUtc(imagePath)) return processed;

        var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(imagePath));
        using (var input = await source.OpenReadAsync())
        {
            var decoder = await BitmapDecoder.CreateAsync(input);
            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                new BitmapTransform { Flip = flip ? BitmapFlip.Horizontal : BitmapFlip.None },
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            var pixels = pixelData.DetachPixelData();
            Bury(pixels, (int) decoder.PixelWidth, (int) decoder.PixelHeight, bury, feather);

            var folder = await StorageFolder.GetFolderFromPathAsync(cache);
            var target = await folder.CreateFileAsync(Path.GetFileName(processed), CreationCollisionOption.ReplaceExisting);
            using var output = await target.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.PixelWidth, decoder.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();
        }

        return processed;
    }

    // the bottom rows go, the rows above fade in from nothing to solid (smoothly, so there's no visible line)
    private static void Bury(byte[] bgra, int width, int height, int bury, int feather)
    {
        if (bury <= 0) return;

        var groundLine = Math.Max(0, height - bury);
        var fadeTop = Math.Max(0, groundLine - feather);
        for (var y = fadeTop; y < height; y++)
        {
            double keep;
            if (y >= groundLine) keep = 0;
            else
            {
                var t = (groundLine - y) / (double) Math.Max(1, feather);
                keep = t * t * (3 - 2 * t);
            }

            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4 + 3;
                bgra[i] = (byte) Math.Round(bgra[i] * keep);
            }
        }
    }

    #endregion

    #region Cache

    private Dictionary<string, BitmapImage> ImagesCache { get; } = new();

    private Dictionary<string, BitmapImage> AnimationsCache { get; } = new();

    #endregion
}
