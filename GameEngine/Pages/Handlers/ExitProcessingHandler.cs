using GameLibrary.Models.Actions;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Exit Processing: ends the page, nothing after it runs.
/// </summary>
public class ExitProcessingHandler : ActionHandler<ExitProcessingAction>
{
    protected override ActionResult Start(PageRun run, ExitProcessingAction action)
    {
        run.Lists.Clear();
        return ActionResult.Done;
    }
}
