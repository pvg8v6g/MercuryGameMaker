using System.Diagnostics;
using GameLibrary.Models.GameMaps;
using GameMaker.Services.Navigation;
using GameMaker.Tasks;

namespace GameMaker.Services.Testing;

public class TestLaunchService(INavigationService navigationService) : ITestLaunchService
{
    #region Properties

    // the engine is copied next to the editor when it builds (see BundleGameEngine in the project file)
    private static string EnginePath => Path.Combine(AppContext.BaseDirectory, "Engine", "GameEngine.exe");

    public bool CanLaunch => File.Exists(EnginePath);

    #endregion

    #region Methods

    public async Task Launch(GameMap map, bool windowed)
    {
        if (!CanLaunch) return;

        await navigationService.ShowProgressPopup<SaveDataTask>("Saving Game Files");

        var startInfo = new ProcessStartInfo(EnginePath)
        {
            WorkingDirectory = Path.GetDirectoryName(EnginePath),
            UseShellExecute = false
        };

        // the engine's StartService reads this, it opens the map at its test spawn
        startInfo.ArgumentList.Add("--test");
        startInfo.ArgumentList.Add(map.Guid.ToString());
        if (windowed) startInfo.ArgumentList.Add("--windowed");
        Process.Start(startInfo);
    }

    #endregion
}
