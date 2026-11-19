using GameLibrary.Enumerations;
using GameLibrary.Models;

namespace GameEngine.Models;

/// <summary>
/// An entity a page sent to another map: its saved entity (unscaled, as the editor made it) and the tile it stands on
/// there. It shows up on that map every time the map loads, and no longer on its own map.
/// </summary>
public record VisitingEntity(Entity Entity, int TileX, int TileY, Direction Direction);
