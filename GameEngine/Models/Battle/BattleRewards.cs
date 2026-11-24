using GameLibrary.Models;

namespace GameEngine.Models.Battle;

/// <summary>
/// Everything a won battle handed out, gathered as it happens so the victory screen can list it: the EXP and what it did
/// to each party member, and the money and items the party picked up off the map (what wasn't picked up is lost, so it's
/// never here).
/// </summary>
public class BattleRewards
{
    #region Properties

    // the EXP each party member who wasn't down got
    public int Experience { get; set; }

    public int Money { get; set; }

    public List<Member> Members { get; } = [];

    // one row per kind of item, however many were picked up
    public List<Item> Items { get; } = [];

    #endregion

    #region Methods

    public void Clear()
    {
        Experience = 0;
        Money = 0;
        Members.Clear();
        Items.Clear();
    }

    public void AddItem(Guid id, string name, GameIcon icon, int amount)
    {
        if (Items.FirstOrDefault(x => x.Id == id) is { } known) known.Count += amount;
        else Items.Add(new Item(id, name, icon) { Count = amount });
    }

    #endregion

    #region Types

    // a party member at the end of the fight. A fallen one got nothing
    public record Member(string Name, string? FaceName, int? FaceIndex, int Experience, int LevelBefore, int LevelAfter, bool Fallen);

    public class Item(Guid id, string name, GameIcon icon)
    {
        public Guid Id => id;

        public string Name => name;

        public GameIcon Icon => icon;

        public int Count { get; set; }
    }

    #endregion
}
