using GameEngine.Battle.Controllers;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Fighter;

namespace GameEngine.Models.Battle;

/// <summary>
/// A fighter in the battle: the body walking on the battle map and the stats it fights with. A party member's stats are
/// the actor itself, so Life and Mana spent in the fight stay spent afterwards. An enemy's are a fresh copy of the enemy
/// at full Life and Mana. Team is who it fights for right now, Original Team who it started with (states like Charmed
/// change Team for a while and put it back).
/// </summary>
public class BattleFighter(MapEntity body, Fighter stats, bool isEnemy, bool isLeader, int team)
{
    #region Constants

    public const int PartyTeam = 0;

    public const int EnemyTeam = 1;

    #endregion

    #region Properties

    public MapEntity Body => body;

    public Fighter Stats => stats;

    public bool IsEnemy => isEnemy;

    // the party member the player controls
    public bool IsLeader => isLeader;

    public bool IsBoss => isEnemy && stats.Boss;

    public int Team { get; set; } = team;

    public int OriginalTeam { get; } = team;

    // how likely enemies are to pick this fighter as their target, it only resets when the battle ends
    public double Threat { get; set; }

    // the artes on its keys, slot 0 first (the weapon attack). Empty slots are null
    public Arte?[] Slots { get; set; } = [];

    // updates left until each arte can be used again, and how long that cooldown was, for the HUD's clock wipe
    public Dictionary<Guid, (int Left, int Total)> Cooldowns { get; } = [];

    // the tiny cooldown after any arte, so one key press can never fire two
    public int GlobalCooldown { get; set; }

    // the arte being cast, and the updates left on the cast (the fighter can't move until it's done)
    public Arte? Casting { get; set; }

    public int CastLeft { get; set; }

    public int CastTotal { get; set; }

    // Life reached 0: Incapacitated. Nothing but a revive can touch it
    public bool IsDown { get; set; }

    // who decides what it does: the player for the leader, the AI for everyone else
    public IFighterController? Controller { get; set; }

    // the states on it right now
    public List<BattleState> States { get; } = [];

    // what the AI keeps between updates
    public BattleAiState Ai { get; } = new();

    #endregion
}
