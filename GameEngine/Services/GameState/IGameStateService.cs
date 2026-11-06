using GameEngine.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Services.GameState;

public interface IGameStateService
{
    bool IsTransferring { get; set; }

    // set when the player walks off a connected edge, the scene loads the map on its next update
    EdgeTransfer? EdgeTransfer { get; set; }

    GameMap? ActiveMap { get; set; }

    MapEntity? Player { get; set; }

    List<MapEntity> MapEntities { get; }

    List<MapArea> MapAreas { get; }

    HashSet<Guid> OwnedItems { get; }

    HashSet<Guid> ActiveParty { get; }

    HashSet<Guid> OwnedParty { get; }

    Dictionary<string, bool> Switches { get; }

    Dictionary<string, string> Variables { get; }
}
