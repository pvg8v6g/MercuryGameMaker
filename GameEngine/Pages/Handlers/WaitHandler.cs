using GameLibrary.Models.Actions;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Wait: holds the page for a time in milliseconds (1000 is a second), counted in game updates.
/// </summary>
public class WaitHandler(IGameDataService gameDataService) : ActionHandler<WaitAction>
{
    protected override ActionResult Start(PageRun run, WaitAction action)
    {
        var updates = gameDataService.Updates(action.WaitTime);
        run.State<WaitState>().Updates = updates;
        return updates > 0 ? ActionResult.Busy : ActionResult.Done;
    }

    protected override ActionResult Update(PageRun run, WaitAction action)
    {
        return --run.State<WaitState>().Updates > 0 ? ActionResult.Busy : ActionResult.Done;
    }

    private class WaitState
    {
        public int Updates { get; set; }
    }
}
