using GameEngine.Models;
using GameEngine.Models.Battle;
using GameLibrary.Models.Weathers;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Services.GameState;

/// <summary>
/// Everything about the game that is being played right now: the map, what is on it, the party and the switches.
/// </summary>
public class GameStateService : IGameStateService
{
    #region Properties

    public bool IsTransferring { get; set; }

    public EdgeTransfer? EdgeTransfer { get; set; }

    public GameMap? ActiveMap { get; set; }

    public BattleRequest? BattleRequest { get; set; }

    public ActiveBattle? Battle { get; set; }

    public HashSet<Guid> InspectedEnemies { get; } = [];

    public MapEntity? Player { get; set; }

    public List<MapEntity> MapEntities { get; } = [];

    public List<MapArea> MapAreas { get; } = [];

    public Dictionary<Guid, int> Items { get; } = [];

    public int Money { get; set; }

    public List<Guid> ActiveParty { get; } = [];

    public Dictionary<Guid, Fighter> Actors { get; } = [];

    public HashSet<Guid> OwnedParty { get; } = [];

    public Dictionary<string, bool> Switches { get; } = [];

    public Dictionary<string, string> Variables { get; } = [];

    public Dictionary<(Guid Map, Guid Entity, SelfSwitch Key), bool> SelfSwitches { get; } = [];

    public Dictionary<(Guid Map, Guid Entity), EntityPlace> EntityPlaces { get; } = [];

    public WeatherSettings? Weather { get; set; }

    public HashSet<(Guid Map, Guid Entity)> DepartedEntities { get; } = [];

    public Dictionary<Guid, List<VisitingEntity>> VisitingEntities { get; } = [];

    public bool IsPlayerBusy { get; set; }

    #endregion

    #region Methods

    // a self switch that was never set is off
    public bool SelfSwitch(MapEntity entity, SelfSwitch key)
    {
        return SelfSwitches.GetValueOrDefault((ActiveMap?.Guid ?? Guid.Empty, entity.Entity.Guid, key));
    }

    public void SetSelfSwitch(MapEntity entity, SelfSwitch key, bool value)
    {
        SelfSwitches[(ActiveMap?.Guid ?? Guid.Empty, entity.Entity.Guid, key)] = value;
    }

    #endregion
}
