using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;

namespace GameEngine.Models.Battle;

/// <summary>
/// The battle being fought, and everything needed to put the player back afterwards: the story map they came from,
/// exactly where they stood (unscaled map pixels, the sprite's top left) and the way they faced, and their story-map
/// walking speed. The speed is put back exactly as it was saved, never worked out from the battle speed, so nothing
/// that changed speed during the fight can follow the player out of it.
/// </summary>
public class ActiveBattle(BattleRequest request, GameMap returnMap, double returnX, double returnY, Direction returnDirection, int storySpeed)
{
    public BattleRequest Request => request;

    public GameMap ReturnMap => returnMap;

    public double ReturnX => returnX;

    public double ReturnY => returnY;

    public Direction ReturnDirection => returnDirection;

    public int StorySpeed => storySpeed;

    // the fight is over and the party is on its way back to the story map
    public bool IsEnding { get; set; }

    // everyone in the fight, party first (the leader first of all), then the enemies. Filled once the battle map is up
    public List<BattleFighter> Fighters { get; } = [];
}
