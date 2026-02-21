using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    // string GetIconPath();
    // Task<ImageSource> GetGameIcon();
    // Task<Rect> GetIconViewport(int index);
    // string GetEngineIconPath();
    // Task<ImageSource> GetEngineIcon();
    // Task<Rect> GetEngineIconViewport(int index);
    string GetIconsPath();
    Task<CroppedImage> GetIcon(int index);
    Task<CroppedImage> GetEngineIcon(int index);
    Task<BitmapImage> GetImage(string imagePath);
    Task<(double width, double height)> GetSegmentation(string fileName);
    (double x, double y) GetCharacterDivisions(string fileName);
}
