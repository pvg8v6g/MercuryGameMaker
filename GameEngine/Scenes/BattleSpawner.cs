using Windows.Foundation;
using GameEngine.Services.GameState;
using GameEngine.Services.Party;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Troops;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameEngine.Scenes;

/// <summary>
/// Puts a troop's battle together: up to four party members on the troop's party spawn points (in party order, or
/// shuffled) and the troop's enemies on their own spots (Fixed) or on its enemy spawn points (Random, a different one
/// each). Everyone stands with their feet on their tile. Spots are unscaled battle map pixels, the scene scales them.
/// </summary>
public class BattleSpawner(IGameDataService gameDataService, IGameStateService gameStateService, IJsonService jsonService, PartyService partyService, PlayerSpawner playerSpawner)
{
    #region Constants

    // the most party members that fight at once
    private const int PartyBattlers = 4;

    #endregion

    #region Fields

    private readonly Random _random = new();

    #endregion

    #region Methods

    // where the leader (the player) stands, and everyone else's bodies
    public async Task<BattleLineup> Lineup(Troop troop)
    {
        var tile = gameDataService.GridSize;
        var taken = new HashSet<(int, int)>();

        // the party, in party order: the first is the leader the player controls
        var party = gameStateService.ActiveParty.Select(id => partyService.Actor(id)).OfType<Fighter>().Take(PartyBattlers).ToArray();
        var partySpots = troop.PartySpawn == SpawnMode.Random ? Shuffle(troop.PartySpawnPoints) : [.. troop.PartySpawnPoints];
        var partyTiles = party.Select((_, i) => i < partySpots.Count ? Take(taken, partySpots[i].X, partySpots[i].Y) : Nearest(taken, troop.PartySpawnPoints, tile)).ToArray();

        // the enemies: fixed ones first so a random one never lands on a boss's spot
        var enemies = troop.Enemies
            .Select(x => (troopEnemy: x, fighter: gameDataService.Enemies.FirstOrDefault(e => e.Guid == x.EnemyGuid)))
            .Where(x => x.fighter is not null)
            .ToArray();
        var enemyTiles = new (int x, int y)[enemies.Length];
        for (var i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].troopEnemy is { Spawn: SpawnMode.Fixed, X: { } x, Y: { } y }) enemyTiles[i] = Take(taken, x, y);
        }

        var randomSpots = new Queue<TroopSpawnPoint>(Shuffle(troop.EnemySpawnPoints));
        for (var i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].troopEnemy is { Spawn: SpawnMode.Fixed, X: not null, Y: not null }) continue;
            enemyTiles[i] = NextFree(taken, randomSpots) ?? Nearest(taken, troop.EnemySpawnPoints, tile);
        }

        // the party faces up the field toward the enemies, the enemies face down toward the party
        var bodies = new List<Entity>();
        var roster = new List<BattleRosterEntry>();
        Point? leaderAt = null;
        for (var i = 0; i < party.Length; i++)
        {
            var (x, y) = await playerSpawner.StandAt(party[i].CharacterName, partyTiles[i].x, partyTiles[i].y);
            if (i == 0)
            {
                // the leader is the player, the scene makes its body
                leaderAt = new Point(x, y);
                roster.Add(new BattleRosterEntry(null, party[i], false));
                continue;
            }

            var body = await playerSpawner.CreateFighter(party[i], x, y, Direction.Up, "Party Member");
            bodies.Add(body);
            roster.Add(new BattleRosterEntry(body, party[i], false));
        }

        for (var i = 0; i < enemies.Length; i++)
        {
            var fighter = enemies[i].fighter!;
            var (x, y) = await playerSpawner.StandAt(fighter.CharacterName, enemyTiles[i].x, enemyTiles[i].y);
            var body = await playerSpawner.CreateFighter(fighter, x, y, Direction.Down, "Enemy");
            bodies.Add(body);

            // every enemy fights as its own fresh copy, at full Life and Mana
            var stats = jsonService.Copy(fighter) ?? fighter;
            stats.Life = stats.MaxLife;
            stats.Mana = stats.MaxMana;
            roster.Add(new BattleRosterEntry(body, stats, true));
        }

        return new BattleLineup(leaderAt, Direction.Up, bodies, roster);
    }

    #endregion

    #region Private Methods

    private (int x, int y) Take(HashSet<(int, int)> taken, int x, int y)
    {
        taken.Add((x, y));
        return (x, y);
    }

    private (int x, int y)? NextFree(HashSet<(int, int)> taken, Queue<TroopSpawnPoint> spots)
    {
        while (spots.TryDequeue(out var spot))
        {
            if (!taken.Contains((spot.X, spot.Y))) return Take(taken, spot.X, spot.Y);
        }

        return null;
    }

    // a troop short of spawn points (the Troops page warns about it): the nearest open tile beside the side's other
    // points, rings of tiles further and further out, so the battle can still start
    private (int x, int y) Nearest(HashSet<(int, int)> taken, IReadOnlyList<TroopSpawnPoint> side, int tile)
    {
        var (startX, startY) = side.Count > 0 ? (side[0].X, side[0].Y) : (tile * 10, tile * 10);
        for (var ring = 1; ring < 30; ring++)
        {
            for (var dy = -ring; dy <= ring; dy++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;

                    var spot = (startX + (dx * tile), startY + (dy * tile));
                    if (spot.Item1 >= 0 && spot.Item2 >= 0 && !taken.Contains(spot)) return Take(taken, spot.Item1, spot.Item2);
                }
            }
        }

        return Take(taken, startX, startY);
    }

    private List<TroopSpawnPoint> Shuffle(IEnumerable<TroopSpawnPoint> points)
    {
        return [.. points.OrderBy(_ => _random.Next())];
    }

    #endregion
}

// the leader's spot (the sprite's top left, unscaled) and facing, the bodies of everyone else in the battle, and who
// fights with which stats (the leader's body is the player, so it has none here)
public record BattleLineup(Point? LeaderAt, Direction LeaderFacing, List<Entity> Bodies, List<BattleRosterEntry> Roster);

public record BattleRosterEntry(Entity? Body, Fighter Stats, bool IsEnemy);
