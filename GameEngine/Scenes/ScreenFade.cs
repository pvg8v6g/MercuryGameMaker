namespace GameEngine.Scenes;

/// <summary>
/// The screen faded by a page (Screen Fade), on top of the fade the scene does while changing maps. A screen faded
/// out by a page stays black through a map change until a page fades it back in.
/// </summary>
public class ScreenFade
{
    #region Fields

    private double _target;
    private double _step;

    #endregion

    #region Properties

    // 0 is the map as it is, 1 is a black screen
    public double Level { get; private set; }

    public bool IsFading => Math.Abs(Level - _target) > 0.0001;

    #endregion

    #region Methods

    // fades to black (out) or back to the map (in) over this many updates, 0 is straight away
    public void FadeTo(bool black, int updates)
    {
        _target = black ? 1.0 : 0.0;
        if (updates <= 0)
        {
            Level = _target;
            return;
        }

        _step = Math.Abs(_target - Level) / updates;
    }

    // once an update
    public void Update()
    {
        if (!IsFading) return;

        Level = Level < _target ? Math.Min(_target, Level + _step) : Math.Max(_target, Level - _step);
    }

    #endregion
}
