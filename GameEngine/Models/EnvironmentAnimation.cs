namespace GameEngine.Models;

/// <summary>
/// A playing environment (water, lava, fog). Every frame is the same picture tiled over the map,
/// only the current frame's tiles are drawn.
/// </summary>
public class EnvironmentAnimation(SceneSprite[][] frames)
{
    #region Constants

    // game updates each frame is shown for
    private const int FrameUpdates = 3;

    #endregion

    #region Fields

    private int _timer;
    private int _frame;

    #endregion

    #region Properties

    public SceneSprite[] Current => frames[_frame];

    // the first tile of every frame, one picture for each image the animation shows
    public IEnumerable<SceneSprite> Frames => frames.Where(x => x.Length > 0).Select(x => x[0]);

    #endregion

    #region Methods

    public void Update()
    {
        if (++_timer < FrameUpdates) return;
        _timer = 0;
        _frame = (_frame + 1) % frames.Length;
    }

    #endregion
}
