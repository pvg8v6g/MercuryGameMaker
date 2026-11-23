using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Troops;

namespace GameEngine.Models.Battle;

/// <summary>
/// Initiate Battle asked for a fight: the troop, the battle map it's fought on, and whether it can be fled or lost.
/// The scene picks it up on its next update, fades out and loads the battle.
/// </summary>
public record BattleRequest(Troop Troop, GameMap BattleMap, bool CanRun, bool CanLose);
