using GameEngine.Services.GameState;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameEngine.Services.Party;

/// <summary>
/// The actors as they are in this game. Each actor is a copy of its game data made the first time it's needed, pages
/// change the copy (names, levels, life, equipment) and the game data stays as it was made in the editor.
/// </summary>
public class PartyService(IGameDataService gameDataService, IGameStateService gameStateService, IJsonService jsonService)
{
    #region Constants

    private const int MinLevel = 1;
    private const int MaxLevel = 99;

    #endregion

    #region Events

    // the leader changed, or the leader's character image did: the player's sprite follows
    public event EventHandler? LeaderChanged;

    #endregion

    #region Properties

    public Guid? LeaderId => gameStateService.ActiveParty.Count > 0 ? gameStateService.ActiveParty[0] : null;

    public Fighter? Leader => Actor(LeaderId);

    #endregion

    #region Methods

    // the actor's copy in this game, null when the game data has no such actor
    public Fighter? Actor(Guid? id)
    {
        if (id is not { } guid) return null;
        if (gameStateService.Actors.TryGetValue(guid, out var actor)) return actor;

        return Reset(guid);
    }

    // the actor back to how it was made in the editor
    public Fighter? Reset(Guid id)
    {
        if (gameDataService.Actors.FirstOrDefault(x => x.Guid == id) is not { } data || jsonService.Copy(data) is not { } copy) return null;

        gameStateService.Actors[id] = copy;
        return copy;
    }

    // the picked actor, or the whole party when none is picked
    public IEnumerable<Fighter> Targets(Guid? id)
    {
        if (id is not null) return Actor(id) is { } actor ? [actor] : [];

        return gameStateService.ActiveParty.Select(x => Actor(x)).OfType<Fighter>().ToArray();
    }

    public void AddToParty(Guid id, bool initialize)
    {
        var leader = LeaderId;
        if (initialize) Reset(id);
        else Actor(id);

        if (!gameStateService.ActiveParty.Contains(id)) gameStateService.ActiveParty.Add(id);
        gameStateService.OwnedParty.Add(id);
        if (LeaderId != leader) LeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveFromParty(Guid id)
    {
        var leader = LeaderId;
        gameStateService.ActiveParty.Remove(id);
        gameStateService.OwnedParty.Remove(id);
        if (LeaderId != leader) LeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    // the actor's look changed, the player redraws when it's the leader
    public void ImageChanged(Guid id)
    {
        if (LeaderId == id) LeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    // a new level, and the life, mana and attributes its discipline's growths give at that level (static stats stay put)
    public void SetLevel(Fighter actor, int level)
    {
        actor.Level = Math.Clamp(level, MinLevel, MaxLevel);
        if (actor.StaticStats || gameDataService.Disciplines.FirstOrDefault(x => x.Guid == actor.DisciplineGuid) is not { } discipline) return;

        if (Grown(discipline.LifeGrowthGuid, actor.Level) is { } life) actor.MaxLife = life;
        if (Grown(discipline.ManaGrowthGuid, actor.Level) is { } mana) actor.MaxMana = mana;
        foreach (var (attribute, growth) in discipline.AttributeGrowths)
        {
            if (Grown(growth, actor.Level) is { } value) actor.Attributes[attribute] = value;
        }

        actor.Life = Math.Min(actor.Life, actor.MaxLife);
        actor.Mana = Math.Min(actor.Mana, actor.MaxMana);
    }

    // one of the party's items in or out, an item it has none of left is gone from the list
    public void ChangeItem(Guid id, int amount)
    {
        var count = Math.Max(0, gameStateService.Items.GetValueOrDefault(id) + amount);
        if (count == 0) gameStateService.Items.Remove(id);
        else gameStateService.Items[id] = count;
    }

    #endregion

    #region Private Methods

    private int? Grown(Guid growthId, int level)
    {
        if (gameDataService.Growths.FirstOrDefault(x => x.Guid == growthId) is not { } growth) return null;
        return growth.GrowthValues.TryGetValue(level, out var value) ? value : null;
    }

    #endregion
}
