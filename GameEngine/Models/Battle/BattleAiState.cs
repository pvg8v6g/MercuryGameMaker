using Windows.Foundation;
using GameEngine.Navigation;
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

    // the path it's walking (see BattleWalk): its cells, how far along it is, how old it is and where it was going. WalkWait
    // is how long it still stands after being blocked
    public List<Direction>? Path { get; set; }

    public int PathIndex { get; set; }

    public int PathAge { get; set; }

    public Point PathTo { get; set; }

    public WalkState Walk { get; } = new();

    public Direction? LastWay { get; set; }

    public int WalkWait { get; set; }

    // the spot beside its target that it claimed, so its allies pick others, and whether it's standing there
    public BattleFighter? ClaimTarget { get; set; }

    public int ClaimSpot { get; set; }

    public int ClaimAge { get; set; }

    public bool AtSpot { get; set; }

    // the most Threat anyone had when it last picked, so it notices someone's jump
    public double TopThreat { get; set; }

    // how long a healer has been going after an ally without using anything, and how long it's given up on that for
    public int HelpingFor { get; set; }

    public int HelpRest { get; set; }

    // walking away from an enemy that's casting
    public bool Fleeing { get; set; }

    // updates left of a temper, and what it watches for one
    public int SpicyFor { get; set; }

    public int? LastLife { get; set; }

    public int LastFallen { get; set; }
}
