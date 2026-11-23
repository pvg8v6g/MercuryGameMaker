using GameLibrary.Enumerations;

namespace GameEngine.Models.Battle;

/// <summary>
/// What an AI controller keeps about one fighter between updates: who it's after, when it re-rolls that, and the
/// little shuffle it does once it's close.
/// </summary>
public class BattleAiState
{
    public BattleFighter? Target { get; set; }

    public int RerollIn { get; set; }

    public int ThinkIn { get; set; }

    public Direction WanderWay { get; set; }

    public int WanderFor { get; set; }
}
