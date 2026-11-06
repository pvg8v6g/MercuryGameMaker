using GameLibrary.Models.GameMaps;

namespace GameEngine.Services.Start;

public interface IStartService
{
    // true when the editor launched the game to test a single map
    bool IsTest { get; }

    // true when started with --windowed: a normal window instead of full screen
    bool IsWindowed { get; }

    // the map the game opens on and where the party leader appears, null when there are no maps
    (GameMap Map, SpawnPoint Spawn)? Find();
}
