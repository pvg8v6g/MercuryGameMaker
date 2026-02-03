using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameLibrary.Services.Graphics;

public interface IGraphicsService
{
    Task<Canvas> GetEngineIconImproved(int index);
    Task<CanvasImageSource> GetEngineIcon(int index);
}
