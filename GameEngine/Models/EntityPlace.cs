using GameLibrary.Enumerations;

namespace GameEngine.Models;

/// <summary>
/// Where an entity stood when the player left its map (scaled map pixels), put back when the map loads again.
/// </summary>
public record EntityPlace(double X, double Y, double Z, double SortZ, Direction Direction);
