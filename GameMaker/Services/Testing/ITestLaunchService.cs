using GameLibrary.Models.GameMaps;

namespace GameMaker.Services.Testing;

/// <summary>
/// Plays a map in the game engine from the editor.
/// </summary>
public interface ITestLaunchService
{
    // the engine is built and copied next to the editor
    bool CanLaunch { get; }

    // saves the game (the engine plays the saved files), then opens the map at its test spawn
    Task Launch(GameMap map, bool windowed);
}
