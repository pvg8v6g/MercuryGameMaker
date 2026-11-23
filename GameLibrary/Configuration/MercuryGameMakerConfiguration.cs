namespace GameLibrary.Configuration;

public class MercuryGameMakerConfiguration
{
    public string? GameDirectory { get; set; }

    // what the game draws over itself when testing a map from the editor, the same for every map
    public bool ShowEntityHitboxes { get; set; }

    public bool ShowAreas { get; set; }

    public bool ShowCollisions { get; set; }

    // an arte's hitboxes flash briefly where it lands in battle
    public bool ShowArteHitboxes { get; set; }

    // a test opens in a window unless this is on
    public bool TestFullScreen { get; set; }

    // the map editor's side panels folded away to their arrows
    public bool ToolRailCollapsed { get; set; }

    public bool MapsPanelCollapsed { get; set; }
}
