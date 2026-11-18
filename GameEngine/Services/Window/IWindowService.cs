namespace GameEngine.Services.Window;

public interface IWindowService
{
    // the window was resized and the game size worked out again
    event EventHandler? GameResized;

    Task BuildGameEngine(Microsoft.UI.Xaml.Window window);
}
