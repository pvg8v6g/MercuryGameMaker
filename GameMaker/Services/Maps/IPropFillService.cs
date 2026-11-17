using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Props;

namespace GameMaker.Services.Maps;

/// <summary>
/// Fills a map with props by a fill set's rules, reading where things can go from the map:
/// its collision areas (unpassable), its named areas and the colours of its ground image.
/// </summary>
public interface IPropFillService
{
    // new props for the map (not added to it yet), the same seed always gives the same layout
    Task<List<MapProp>> Fill(GameMap map, FillSet fillSet, int seed, IEnumerable<MapProp> keepClearOf);
}
