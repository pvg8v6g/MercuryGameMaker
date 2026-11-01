using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Modules;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Models;

/// <summary>
/// An entity while it is on the active map. The entity is the saved data, everything else here is the
/// state the engine keeps while the map is running (active page, picture, movement, hitboxes).
/// </summary>
public class MapEntity(Entity entity)
{
    #region Fields

    private readonly Dictionary<Area, MapHitbox> _hitboxes = [];

    #endregion

    #region Properties

    public Entity Entity => entity;

    // the highest page whose conditions pass, the active page's modules are the entity's modules
    public Page? ActivePage { get; set; }

    public IEnumerable<Module> Modules => ActivePage?.Modules ?? Enumerable.Empty<Module>();

    // the character sheet frame, starts at the module's index and changes while walking
    public int Frame { get; set; }

    public int SteppingIndex { get; set; }

    public PictureBox? Picture { get; set; }

    // the character image the picture was made from, a page change can swap it
    public string? PicturePath { get; set; }

    // where the picture's top left sits on the map before the camera moves it
    public double PictureX { get; set; }

    public double PictureY { get; set; }

    public MovementState Movement { get; } = new();

    #endregion

    #region Methods

    public T? GetModule<T>() where T : Module => Modules.OfType<T>().FirstOrDefault();

    public bool HasModule<T>() where T : Module => GetModule<T>() is not null;

    // the running copy of a saved hitbox, positioned and scaled every update
    public MapHitbox GetHitbox(Area area)
    {
        if (_hitboxes.TryGetValue(area, out var hitbox)) return hitbox;
        hitbox = new MapHitbox(area);
        _hitboxes[area] = hitbox;
        return hitbox;
    }

    #endregion
}
