using GameEngine.Scenes;

namespace GameEngine.Interface;

/// <summary>
/// Every screen layer, bottom first in the order they're registered. The game pauses while any open layer pauses it.
/// </summary>
public class ScreenLayers(IEnumerable<ScreenLayer> layers)
{
    #region Fields

    private readonly ScreenLayer[] _layers = [.. layers];

    #endregion

    #region Properties

    public bool IsPaused => _layers.Any(x => x is { IsOpen: true, PausesGame: true });

    #endregion

    #region Methods

    // called from the game loop once an update
    public void Update(GameCamera camera)
    {
        foreach (var layer in _layers) layer.Update(camera);
    }

    #endregion
}
