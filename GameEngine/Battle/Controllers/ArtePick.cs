using GameLibrary.Enumerations;
using GameLibrary.Models.Artes;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// An arte whose "pick me if" passed, the way to face for it, and how likely it is to be drawn among the others.
/// </summary>
public record ArtePick(Arte Arte, Direction Facing, double Weight);
