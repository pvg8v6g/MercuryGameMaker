using GameEngine.Models;
using GameLibrary.Enumerations;
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

    // each entity's self switches, kept by map and entity so they last when the map is left and loaded again
    Dictionary<(Guid Map, Guid Entity, SelfSwitch Key), bool> SelfSwitches { get; }

    // a page the player started (or an autorun) is running, the player can't walk or start another
    bool IsPlayerBusy { get; set; }

    bool SelfSwitch(MapEntity entity, SelfSwitch key);

    void SetSelfSwitch(MapEntity entity, SelfSwitch key, bool value);
}
