using GameLibrary.Models.Actions;

namespace GameEngine.Pages;

/// <summary>
/// Runs a page's actions in order, each by the handler for its type. Most actions are done at once and the next one
/// runs in the same update, an action that takes time (a wait) holds the page until its handler says it's done.
/// Actions with no handler yet are skipped. Map pages and conversations both run through here.
/// </summary>
public class ActionRunner(IEnumerable<IActionHandler> handlers)
{
    #region Constants

    // a page that loops on instant actions (a branch that never waits) still lets the game draw a frame
    private const int MaxActionsPerUpdate = 1000;

    #endregion

    #region Fields

    private readonly Dictionary<Type, IActionHandler> _handlers = handlers.ToDictionary(x => x.ActionType);

    #endregion

    #region Methods

    public void Update(PageRun run)
    {
        if (run.Busy is { } busy)
        {
            if (Handler(busy)?.Update(run, busy) == ActionResult.Busy) return;
            run.Busy = null;
        }

        for (var count = 0; count < MaxActionsPerUpdate && run.Lists.TryPeek(out var list); count++)
        {
            if (list.Index >= list.Actions.Count)
            {
                run.Lists.Pop();
                continue;
            }

            var action = list.Actions[list.Index++];
            if (Handler(action)?.Start(run, action) != ActionResult.Busy) continue;

            run.Busy = action;
            return;
        }
    }

    #endregion

    #region Private Methods

    private IActionHandler? Handler(PageAction action) => _handlers.GetValueOrDefault(action.GetType());

    #endregion
}
