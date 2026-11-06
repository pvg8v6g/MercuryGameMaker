using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GameLibrary.Attributes;
using MercuryLibrary.Enumerations;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// An animated image shared by every map (water, lava, fog), tiled over the whole map.
/// The frames live in Graphics\Maps\Environment as {Name}_0.png, {Name}_1.png ... or a single {Name}.png.
/// A map draws an environment when it has one in its environments list.
/// </summary>
public partial class MapEnvironment(string name) : MapImageSettings
{
    #region Constants

    private static readonly MapLayerAttribute Default = new() { Z = -2, BlendMode = BlendMode.LinearDodge };

    #endregion

    #region Properties

    public string Name => name;

    // ignored here as well, System.Text.Json reads attributes off the override and not the abstract property
    [JsonIgnore]
    public override MapLayerAttribute DefaultSettings => Default;

    #endregion

    #region Methods

    // the frames in play order, empty when the environment's images are gone
    public string[] FramePaths(string graphicsDirectory)
    {
        var directory = Folder(graphicsDirectory);
        if (!Directory.Exists(directory)) return [];

        var frames = Directory.GetFiles(directory, "*.png")
            .Select(path => (path, match: FrameName().Match(Path.GetFileNameWithoutExtension(path))))
            .Where(x => x.match.Success && x.match.Groups[1].Value == name)
            .OrderBy(x => int.Parse(x.match.Groups[2].Value))
            .Select(x => x.path)
            .ToArray();
        if (frames.Length > 0) return frames;

        var still = Path.Combine(directory, $"{name}.png");
        return File.Exists(still) ? [still] : [];
    }

    // every environment found in the folder, Water_0.png ... Water_47.png is one environment called Water
    public static string[] Names(string graphicsDirectory)
    {
        var directory = Folder(graphicsDirectory);
        if (!Directory.Exists(directory)) return [];

        return
        [
            .. Directory.GetFiles(directory, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>()
                .Select(file => FrameName().Match(file) is { Success: true } match ? match.Groups[1].Value : file)
                .Distinct()
                .Order()
        ];
    }

    private static string Folder(string graphicsDirectory) => Path.Combine(graphicsDirectory, "Maps", "Environment");

    [GeneratedRegex(@"^(.+)_(\d+)$")]
    private static partial Regex FrameName();

    #endregion
}
