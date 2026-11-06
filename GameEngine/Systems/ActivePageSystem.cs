using GameEngine.Models;
using GameEngine.Services.Conditions;

namespace GameEngine.Systems;

/// <summary>
/// Picks the highest page whose conditions all pass. The active page's modules decide what every other system does.
/// </summary>
public class ActivePageSystem(IConditionService conditionService) : IEntitySystem
{
    // the page decides what an entity looks like, so it is picked even while paused
    public bool RunsWhilePaused => true;

    public void Update(MapEntity entity)
    {
        entity.ActivePage = entity.Entity.Pages
            .OrderByDescending(x => x.Id)
            .FirstOrDefault(page => page.Conditions.All(condition => conditionService.Passed(condition, page)));
    }
}
