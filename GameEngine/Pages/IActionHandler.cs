using GameLibrary.Models.Actions;

namespace GameEngine.Pages;

/// <summary>
/// Runs one type of page action. Register a handler and the action runner uses it for every action of its type,
/// an action with no handler is skipped.
/// </summary>
public interface IActionHandler
{
    Type ActionType { get; }

    // runs the action, busy holds the page on it
    ActionResult Start(PageRun run, PageAction action);

    // asked once an update while the action is busy, until it's done
    ActionResult Update(PageRun run, PageAction action);
}

public enum ActionResult
{
    // the page goes on to the next action straight away
    Done,

    // the page waits on this action (a wait, a movement route, a text window)
    Busy
}

/// <summary>
/// A handler for one action type, so a handler works with its own action instead of casting.
/// </summary>
public abstract class ActionHandler<T> : IActionHandler where T : PageAction
{
    public Type ActionType => typeof(T);

    public ActionResult Start(PageRun run, PageAction action) => Start(run, (T) action);

    public ActionResult Update(PageRun run, PageAction action) => Update(run, (T) action);

    protected abstract ActionResult Start(PageRun run, T action);

    // an action that never waits is done when it starts
    protected virtual ActionResult Update(PageRun run, T action) => ActionResult.Done;
}
