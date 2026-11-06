using System.Text.Json.Serialization;
using GameLibrary.Attributes;
using GameLibrary.Enumerations;
using MercuryLibrary.Extensions;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// One of a map's own images, loaded from Graphics\Maps\{Type}\map{id}.png (Graphics\Maps\Ground\map1.png).
/// Everything here has a default and only needs changing when a map wants something different.
/// </summary>
public partial class MapLayer(MapLayerType type) : MapImageSettings
{
    #region Properties

    public MapLayerType Type => type;

    // ignored here as well, System.Text.Json reads attributes off the override and not the abstract property
    [JsonIgnore]
    public override MapLayerAttribute DefaultSettings => Defaults(type);

    #endregion

    #region Methods

    public string ImagePath(string graphicsDirectory, int mapId)
    {
        return Path.Combine(graphicsDirectory, "Maps", type.ToString(), $"map{mapId}.png");
    }

    // the defaults come from the MapLayer attribute on each MapLayerType
    public static MapLayerAttribute Defaults(MapLayerType type) => type.GetAttribute<MapLayerAttribute>() ?? new MapLayerAttribute();

    #endregion
}
