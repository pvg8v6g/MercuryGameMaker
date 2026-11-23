using GameEngine.Models;
using GameEngine.Models.Battle;
using GameLibrary.Models.Weathers;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Services.GameState;

public interface IGameStateService
{
    bool IsTransferring { get; set; }

    // set when the player walks off a connected edge, the scene loads the map on its next update
    EdgeTransfer? EdgeTransfer { get; set; }

    GameMap? ActiveMap { get; set; }

    // set by Initiate Battle, the scene starts the battle on its next update
    BattleRequest? BattleRequest { get; set; }

    // the battle being fought, null on the story maps
    ActiveBattle? Battle { get; set; }

    // the enemies the player has inspected (their bestiary entry is filled in), their Life bars show in battle
    HashSet<Guid> InspectedEnemies { get; }

    MapEntity? Player { get; set; }

    List<MapEntity> MapEntities { get; }

    List<MapArea> MapAreas { get; }

    // how many of each item (consumable or equipment) the party carries, an item it has none of isn't listed
    Dictionary<Guid, int> Items { get; }

    int Money { get; set; }

    // the party walking around, in order: the first is the leader the player controls
    List<Guid> ActiveParty { get; }

    // the actors as they are in this game (level, life, equipment, names changed by pages), made from the game data
    // the first time each one is needed
    Dictionary<Guid, Fighter> Actors { get; }

    HashSet<Guid> OwnedParty { get; }

    Dictionary<string, bool> Switches { get; }

    Dictionary<string, string> Variables { get; }

    // each entity's self switches, kept by map and entity so they last when the map is left and loaded again
    Dictionary<(Guid Map, Guid Entity, SelfSwitch Key), bool> SelfSwitches { get; }

    // where entities stood when their map was left, by map and entity, for pages that remember their position
    Dictionary<(Guid Map, Guid Entity), EntityPlace> EntityPlaces { get; }

    // the world's weather, set by Set Weather and kept from map to map (a map with its own weather shows that instead)
    WeatherSettings? Weather { get; set; }

    // entities a page sent away from their own map, by the map they left
    HashSet<(Guid Map, Guid Entity)> DepartedEntities { get; }

    // the entities a page sent to each map, by the map they went to
    Dictionary<Guid, List<VisitingEntity>> VisitingEntities { get; }

    // a page the player started (or an autorun) is running, the player can't walk or start another
    bool IsPlayerBusy { get; set; }

    bool SelfSwitch(MapEntity entity, SelfSwitch key);

    void SetSelfSwitch(MapEntity entity, SelfSwitch key, bool value);
}
