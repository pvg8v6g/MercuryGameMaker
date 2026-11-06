using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;

namespace GameEngine.Services.Start;

/// <summary>
/// Decides where the game opens. A real game opens at the game start, the editor's Test button launches
/// the engine with "--test {map guid}" to open that map at its test spawn instead.
/// </summary>
public class StartService(IGameDataService gameDataService) : IStartService
{
    #region Constants

    public const string TestArgument = "--test";

    public const string WindowedArgument = "--windowed";

    #endregion

    #region Fields

    private readonly Guid? _testMap = ReadTestMap();

    #endregion

    #region Properties

    public bool IsTest => _testMap is not null;

    public bool IsWindowed { get; } = Environment.GetCommandLineArgs().Contains(WindowedArgument);

    #endregion

    #region Methods

    public (GameMap Map, SpawnPoint Spawn)? Find()
    {
        var start = gameDataService.GameStart;
        if (_testMap is { } testGuid && Playable(testGuid) is { } testMap)
        {
            // a map without a test spawn falls back to the game start when it is on this map, else the top left tile
            var spawn = testMap.TestSpawn ?? (start.MapGuid == testMap.Guid ? start.Spawn : new SpawnPoint());
            return (testMap, spawn);
        }

        var map = (start.MapGuid is { } guid ? Playable(guid) : null)
                  ?? gameDataService.GameMaps.Where(x => !x.FolderOnly).MinBy(x => x.Id);
        return map is null ? null : (map, start.MapGuid == map.Guid ? start.Spawn : new SpawnPoint());
    }

    #endregion

    #region Private Methods

    private GameMap? Playable(Guid guid) => gameDataService.GameMaps.FirstOrDefault(x => x.Guid == guid && !x.FolderOnly);

    private static Guid? ReadTestMap()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, TestArgument);
        return index >= 0 && index + 1 < args.Length && Guid.TryParse(args[index + 1], out var guid) ? guid : null;
    }

    #endregion
}
