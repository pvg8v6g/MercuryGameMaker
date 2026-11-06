using GameLibrary.Models.GameMaps;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// One of the shared environments (water, lava, fog). Turning it on adds it to the map's environments,
/// turning it off removes it along with its settings.
/// </summary>
public partial class MapEnvironmentModel(GameMap map, string name, string graphicsDirectory) : MapImageModel(FindOption(map, name))
{
    #region Properties

    public MapEnvironment Environment => (MapEnvironment) Settings;

    public override string Name => name;

    // the editor shows the first frame, the game animates them all
    public string[] FramePaths { get; } = new MapEnvironment(name).FramePaths(graphicsDirectory);

    public string ImageText => FramePaths.Length == 1 ? "Still" : $"{FramePaths.Length} frames";

    public string ToolTip => $"Loaded from Graphics\\Maps\\Environment\\{name}, tiled over the whole map";

    // the unscaled size of one frame, the editor tiles it over the map
    public (double Width, double Height) FrameSize { get; set; }

    public bool IsOn
    {
        get => map.Environments.Contains(Environment);
        set
        {
            if (value == IsOn) return;
            if (value) map.Environments.Add(Environment);
            else map.Environments.Remove(Environment);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Methods

    // the map's environment by this name, or a new one at the defaults
    private static MapEnvironment FindOption(GameMap map, string name)
    {
        if (map.Environments.FirstOrDefault(x => x.Name == name) is { } option) return option;

        var environment = new MapEnvironment(name);
        environment.Reset();
        return environment;
    }

    #endregion
}
