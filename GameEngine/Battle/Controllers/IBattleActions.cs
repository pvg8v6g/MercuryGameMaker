using Windows.Foundation;
using GameEngine.Models.Battle;
using GameLibrary.Enumerations;
using GameLibrary.Models.Artes;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// What a controller can see and do in the battle. Everything a fighter does goes through here, so the rules
/// (cooldowns, Mana, casting locks, who counts as an enemy) are the same for the player and the AI.
/// </summary>
public interface IBattleActions
{
    // everyone in the fight
    IReadOnlyList<BattleFighter> Fighters { get; }

    // whether the target counts as an enemy to the fighter: the fighter's current team against the target's original one
    bool IsOpponent(BattleFighter fighter, BattleFighter target);

    // ready to use: not casting, not cooling down, enough Mana
    bool CanUse(BattleFighter fighter, Arte arte);

    // uses it now if it's ready
    void Use(BattleFighter fighter, Arte arte);

    // whether the arte, used facing that way from where the fighter stands, would catch the target
    bool WouldHit(BattleFighter fighter, Arte arte, BattleFighter target, Direction facing);

    // takes a step that way (unless locked or mid step), or turns that way without moving
    void Walk(BattleFighter fighter, Direction direction);

    void Face(BattleFighter fighter, Direction direction);

    // the middle of the tile the fighter stands on, in scaled map pixels
    Point Feet(BattleFighter fighter);

    // a tile in scaled map pixels
    double Tile { get; }
}
