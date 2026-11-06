using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// One of the map's own images. The image is found by map id, its settings are only kept in the map's
/// layer options while they differ from the defaults.
/// </summary>
public partial class MapLayerModel(GameMap map, MapLayerType type, string graphicsDirectory) : MapImageModel(FindOption(map, type))
{
    #region Properties

    public MapLayer Layer => (MapLayer) Settings;

    public override string Name => type.ToString();

    public string ImagePath { get; } = new MapLayer(type).ImagePath(graphicsDirectory, map.Id);

    public bool HasImage => File.Exists(ImagePath);

    public string ImageText => HasImage ? Path.GetFileName(ImagePath) : $"No {Path.GetFileName(ImagePath)} found";

    public string ToolTip => $"Loaded from Graphics\\Maps\\{type}\\{Path.GetFileName(ImagePath)}";

    #endregion

    #region Methods

    protected override void OnSettingsChanged()
    {
        // only a layer that differs from its defaults is kept as an option on the map
        if (IsDefault) map.LayerOptions.Remove(Layer);
        else if (!map.LayerOptions.Contains(Layer)) map.LayerOptions.Add(Layer);

        base.OnSettingsChanged();
    }

    // the map's option for this layer, or a new one at the defaults
    private static MapLayer FindOption(GameMap map, MapLayerType type)
    {
        if (map.LayerOptions.FirstOrDefault(x => x.Type == type) is { } option) return option;

        var layer = new MapLayer(type);
        layer.Reset();
        return layer;
    }

    #endregion
}
