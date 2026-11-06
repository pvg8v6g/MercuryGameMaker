using GameLibrary.Models.Areas;

namespace GameEngine.Models;

/// <summary>
/// The running copies of an entity's saved hitboxes.
/// </summary>
public class HitboxState
{
    #region Fields

    private readonly Dictionary<Area, MapHitbox> _hitboxes = [];

    #endregion

    #region Methods

    public MapHitbox Get(Area area)
    {
        if (_hitboxes.TryGetValue(area, out var hitbox)) return hitbox;
        hitbox = new MapHitbox(area);
        _hitboxes[area] = hitbox;
        return hitbox;
    }

    #endregion
}
