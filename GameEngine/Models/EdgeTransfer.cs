using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Models;

/// <summary>
/// The player walked off the edge of the map into the connected map. X and Y are where the player stands on the
/// new map, in unscaled map pixels like the editor saves, and Direction is the way it was walking.
/// On tile: a page transferred the player, X and Y are the tile to stand on.
/// </summary>
public record EdgeTransfer(GameMap Map, double X, double Y, Direction Direction, bool OnTile = false);
