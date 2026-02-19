using Windows.Foundation;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml.Media;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    // string GetIconPath();
    // Task<ImageSource> GetGameIcon();
    // Task<Rect> GetIconViewport(int index);
    // string GetEngineIconPath();
    // Task<ImageSource> GetEngineIcon();
    // Task<Rect> GetEngineIconViewport(int index);
    Task<CroppedImage> GetIcon(int index);
    Task<CroppedImage> GetEngineIcon(int index);
    Task<(double width, double height)> GetSegmentation(string fileName);
    (double x, double y) GetCharacterDivisions(string fileName);
}
