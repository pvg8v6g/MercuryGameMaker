using GameEngine.Models;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Modules;

namespace GameEngine.Services.Modules;

public interface IModuleService
{
    IEnumerable<T> GetMapModules<T>() where T : Module;

    IEnumerable<MapHitbox> GetMapEntityHitboxes();

    IEnumerable<MapHitbox> GetMapShownHitboxes();

    IEnumerable<Area> GetAllHitboxes(EntityHitboxModule hitboxModule, Direction direction);
}
