using GameEngine.Models;

namespace GameEngine.Pages;

/// <summary>
/// The entities touched since the last update: the player walking into one, or one with an event touch page walking
/// into the player. Movement finds them, the page runner starts their pages.
/// </summary>
public class PageTouches
{
    #region Fields

    private readonly HashSet<MapEntity> _touched = [];

    #endregion

    #region Methods

    public void Touch(MapEntity entity) => _touched.Add(entity);

    // the entities touched since the last call
    public MapEntity[] Take()
    {
        var touched = _touched.ToArray();
        _touched.Clear();
        return touched;
    }

    #endregion
}
