using Windows.Foundation;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    string GetEngineIconPath();
    Task<Rect> GetEngineIconViewport(int index);
}
