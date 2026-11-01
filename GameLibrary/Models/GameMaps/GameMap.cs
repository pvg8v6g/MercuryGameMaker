using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Media;

namespace GameLibrary.Models.GameMaps;

public partial class GameMap : BaseModel
{
    #region Properties

    // the java editor stored the map size in pixels at this tile size
    private const int LegacyTileSize = 48;

    /// <summary>
    /// The width of the map in tiles (a 21 tile map at 48px is 1008px wide).
    /// </summary>
    [JsonPropertyName("TileWidth")]
    public int Width
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 21;

    /// <summary>
    /// The height of the map in tiles (a 21 tile map at 48px is 1008px tall).
    /// </summary>
    [JsonPropertyName("TileHeight")]
    public int Height
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 21;

    // reads the pixel sizes saved by the java editor, never written
    [JsonInclude, JsonPropertyName("Width")]
    private int PixelWidth
    {
        set => Width = value / LegacyTileSize;
    }

    [JsonInclude, JsonPropertyName("Height")]
    private int PixelHeight
    {
        set => Height = value / LegacyTileSize;
    }

    // can we run
    public bool DisableDash
    {
        get;
        set => SetField(ref field, value);
    }

    // do we do battle on this map
    public bool BattleMap
    {
        get;
        set => SetField(ref field, value);
    }

    // do weather effects show up on this map
    public bool Interior
    {
        get;
        set => SetField(ref field, value);
    }

    // walking off the absolute edge of the map teleports you to the connected map
    public Guid? North
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? East
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? South
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? West
    {
        get;
        set => SetField(ref field, value);
    }

    // tree folder structure (this is for the game editor only to place maps in a tree structure)
    public Guid? ParentGuid
    {
        get;
        set => SetField(ref field, value);
    }

    // this isn't a real map but a parent only (good for things like "dungeon 1" and then dungeon 1 maps are inside)
    public bool FolderOnly
    {
        get;
        set => SetField(ref field, value);
    }

    // editor only, remembers if the map tree node was left open
    public bool IsExpanded
    {
        get;
        set => SetField(ref field, value);
    }

    // the map images are found by map id in the game engine, these only change a layer's default z, opacity or blend when a map needs to
    public ObservableCollection<MapLayer> LayerOptions { get; init; } = [];

    public AudioFile? Bgm
    {
        get;
        set => SetField(ref field, value);
    }

    public AudioFile? Bgs
    {
        get;
        set => SetField(ref field, value);
    }

    // the entities on this map
    public ObservableCollection<Entity> Entities { get; init; } = [];

    // any areas on this map
    public ObservableCollection<MapArea> Areas { get; init; } = [];

    #endregion
}
