using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Models;

/// <summary>
/// The player walked off the edge of the map into the connected map. X and Y are where the player stands on the
/// new map, in unscaled map pixels like the editor saves, and Direction is the way it was walking.
/// </summary>
public record EdgeTransfer(GameMap Map, double X, double Y, Direction Direction);
