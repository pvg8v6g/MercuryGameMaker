using GameEngine.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Services.GameState;

/// <summary>
/// Everything about the game that is being played right now: the map, what is on it, the party and the switches.
/// </summary>
public class GameStateService : IGameStateService
{
    #region Properties

    public bool IsTransferring { get; set; }

    public GameMap? ActiveMap { get; set; }

    public MapEntity? Player { get; set; }

    public List<MapEntity> MapEntities { get; } = [];

    public List<MapArea> MapAreas { get; } = [];

    public HashSet<Guid> OwnedItems { get; } = [];

    public HashSet<Guid> ActiveParty { get; } = [];

    public HashSet<Guid> OwnedParty { get; } = [];

    public Dictionary<string, bool> Switches { get; } = [];

    public Dictionary<string, string> Variables { get; } = [];

    #endregion
}
