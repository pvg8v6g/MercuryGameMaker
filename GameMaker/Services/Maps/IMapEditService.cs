using GameLibrary.Models;
using GameLibrary.Models.GameMaps;

namespace GameMaker.Services.Maps;

/// <summary>
/// Adding, copying and removing maps in the game data, and keeping map ids unique (a map's images are found by its id).
/// </summary>
public interface IMapEditService
{
    // a new map or folder with the next free id, added to the game
    GameMap Create(bool folder, Guid? parent);

    // a copy taking the id after the original (40 copies to 41), with new guids for it and everything on it
    GameMap? Duplicate(GameMap map);

    // removes the map; maps inside it move up to its parent and connections to it are cleared
    void Delete(GameMap map);

    // when another map has the same id the conflict popup stays up until every id is unique again,
    // false when it was cancelled (the other map's id is put back, the caller decides what happens to this one)
    Task<bool> ResolveIdConflict(GameMap map);

    // a pasted entity is a new entity: new guids for it and its pages, and it belongs to this map
    void ReassignGuids(Entity entity, Guid mapGuid);
}
