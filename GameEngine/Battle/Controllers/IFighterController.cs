using GameEngine.Models.Battle;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// Decides what one fighter does each update: where it walks and which arte it uses. The player's controller reads
/// the keys for the leader; an AI controller drives everyone else, party members and enemies alike. The battle only
/// ever asks a controller, so the real AI system can replace the placeholder without touching the rest, and a state
/// like Charmed can hand the leader to the AI for a while.
/// </summary>
public interface IFighterController
{
    void Update(BattleFighter fighter, IBattleActions battle);
}
