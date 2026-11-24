using GameLibrary.Models;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    string GetIconsFolder();

    int IconSize { get; }

    IReadOnlyList<string> GetIconSheets();

    string GetIconSheetPath(string? sheet);

    // the same sheet with every icon's square background taken off (Graphics\Icons\No Background, same name, cell for
    // cell), for an icon lying on a map. The framed sheet when it has no bare twin
    string GetDropSheetPath(string? sheet);

    string GetBalloonsPath();

    Task<CroppedImage> GetBalloonFrame(int row, int column);

    Task<(int columns, int rows)> GetBalloonDivisions();
    Task<CroppedImage> GetIcon(GameIcon icon);
    Task<CroppedImage> GetEngineIcon(int index);
    string GetCharacterPath();
    Direction GetCharacterDirectionFromIndex(string fileName, int index);
    int GetCharacterIndexFromDirection(string fileName, int index, Direction direction);
    Task<CroppedImage> GetCharacter(string fileName, int index, Direction direction);
    string GetAnimationImagesPath();
    string GetMapGroundPath();
    FileSelection[] GetAnimationImages();
    Task<CroppedImage> GetAnimationImage(string fileName, int frame);
    Task<Rect> GetAnimationViewport(string fileName, int index);
    Task<int> GetAnimationFrames(string fileName);
    Task<(double width, double height)> GetImageDimensions(string imagePath);
    Task<BitmapImage> GetImage(string imagePath, Cache cache = Cache.Regular);
    Task<(double width, double height)> GetSegmentation(string fileName);
    (double x, double y) GetCharacterDivisions(string fileName);
    Task<string> GetPropImagePath(string imagePath, bool flip, int bury, int feather);
}
