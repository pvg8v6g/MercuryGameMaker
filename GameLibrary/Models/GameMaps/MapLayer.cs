using GameLibrary.Attributes;
using GameLibrary.Enumerations;
using MercuryLibrary.Enumerations;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// How a map image is drawn. The image itself is never stored, it is loaded from Graphics\Maps\{Type}\map{id}.png.
/// Everything here has a default and only needs changing when a map wants something different.
/// </summary>
public partial class MapLayer(MapLayerType type) : PropertyChangedUpdater
{
    #region Properties

    public MapLayerType Type => type;

    // draw order, everything on the map (layers and entities) is drawn lowest z first
    public double Z
    {
        get;
        set => SetField(ref field, value);
    }

    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    }

    public BlendMode? MapBlendMode
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    public string ImagePath(string graphicsDirectory, int mapId)
    {
        return Path.Combine(graphicsDirectory, "Maps", type.ToString(), $"map{mapId}.png");
    }

    public void Reset()
    {
        var defaults = Defaults(type);
        Z = defaults.Z;
        Opacity = defaults.Opacity;
        MapBlendMode = defaults.MapBlendMode;
    }

    // the defaults come from the MapLayer attribute on each MapLayerType
    public static MapLayerAttribute Defaults(MapLayerType type) => type.GetAttribute<MapLayerAttribute>() ?? new MapLayerAttribute();

    #endregion
}
