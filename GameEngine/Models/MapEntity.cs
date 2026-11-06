using GameLibrary.Models;
using GameLibrary.Models.Modules;

namespace GameEngine.Models;

/// <summary>
/// An entity while its map is running. The entity is the map's own copy of the saved one, so it can change freely,
/// and each system keeps whatever it needs between updates in a state of its own.
/// </summary>
public class MapEntity(Entity entity)
{
    #region Fields

    private readonly Dictionary<Type, object> _states = [];

    #endregion

    #region Properties

    public Entity Entity => entity;

    // the highest page whose conditions pass, its modules are the entity's modules
    public Page? ActivePage { get; set; }

    public IEnumerable<Module> Modules => ActivePage?.Modules ?? Enumerable.Empty<Module>();

    #endregion

    #region Methods

    public T? GetModule<T>() where T : Module => Modules.OfType<T>().FirstOrDefault();

    public bool HasModule<T>() where T : Module => GetModule<T>() is not null;

    // a system's state for this entity, made the first time the system asks for it
    public T State<T>() where T : class, new()
    {
        if (_states.TryGetValue(typeof(T), out var state)) return (T) state;

        var created = new T();
        _states[typeof(T)] = created;
        return created;
    }

    #endregion
}
