using GameEngine.Models;

namespace GameEngine.Services.Update;

public interface IUpdateService
{
    Task LoadCharacterSheets(IEnumerable<MapEntity> entities);

    void UpdateActivePage(MapEntity entity);

    void UpdateEntity(MapEntity entity);
}
