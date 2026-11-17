namespace GameLibrary.Enumerations;

// where a fill rule puts its props, read from the map's areas
public enum FillPlace
{
    // inside collision areas (a forest, cliffs)
    Unpassable,

    // anywhere a character can walk
    Open,

    // walkable ground close to an unpassable area (bushes along a forest edge)
    NearUnpassable,

    // inside the map areas with the rule's area name (a field, a yard)
    InsideArea
}
