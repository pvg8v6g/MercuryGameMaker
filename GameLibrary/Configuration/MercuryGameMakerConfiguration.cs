namespace GameLibrary.Configuration;

public class MercuryGameMakerConfiguration
{
    public string? GameDirectory { get; set; }

    // what the game draws over itself when testing a map from the editor, the same for every map
    public bool ShowEntityHitboxes { get; set; }

    public bool ShowAreas { get; set; }

    public bool ShowCollisions { get; set; }

    // a test opens in a window unless this is on
    public bool TestFullScreen { get; set; }
}
