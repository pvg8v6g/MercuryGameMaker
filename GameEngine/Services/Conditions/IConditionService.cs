using GameEngine.Models;
using GameLibrary.Models.Conditions;

namespace GameEngine.Services.Conditions;

public interface IConditionService
{
    bool Passed(Condition condition, MapEntity entity);
}
