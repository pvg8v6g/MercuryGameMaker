using GameEngine.Models;

namespace GameEngine.Systems;

/// <summary>
/// One job the map does to its entities every update. Systems run in the order they are registered,
/// each one over every entity before the next starts.
/// </summary>
public interface IEntitySystem
{
    // anything slow (like measuring images) happens here while the map loads, never during an update
    Task Load(IReadOnlyList<MapEntity> entities) => Task.CompletedTask;

    void Update(MapEntity entity);

    // the map is paused while transferring, only systems that draw it (and nothing that plays) keep running
    bool RunsWhilePaused => false;
}
