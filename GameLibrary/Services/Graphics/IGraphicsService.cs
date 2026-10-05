using GameLibrary.Enumerations;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    string GetIconsPath();
    Task<CroppedImage> GetIcon(int index);
    Task<CroppedImage> GetEngineIcon(int index);
    string GetCharacterPath();
    Direction GetCharacterDirectionFromIndex(string fileName, int index);
    int GetCharacterIndexFromDirection(string fileName, int index, Direction direction);
    Task<CroppedImage> GetCharacter(string fileName, int index, Direction direction);
    string GetAnimationImagesPath();
    FileSelection[] GetAnimationImages();
    Task<CroppedImage> GetAnimationImage(string fileName, int frame);
    Task<int> GetAnimationFrames(string fileName);
    Task<(double width, double height)> GetImageDimensions(string imagePath);
    Task<BitmapImage> GetImage(string imagePath, Cache cache = Cache.Regular);
    Task<(double width, double height)> GetSegmentation(string fileName);
    (double x, double y) GetCharacterDivisions(string fileName);
}
