using GameEngine.Models;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Actions;

namespace GameEngine.Pages;

/// <summary>
/// A page running its actions. A branch runs its actions as a list of their own on top of the page's, when that list
/// is done the page carries on after the branch. Handlers keep what they need between updates in a state of their own.
/// </summary>
public class PageRun
{
    #region Fields

    private readonly Dictionary<Type, object> _states = [];

    #endregion

    #region Constructors

    public PageRun(MapEntity entity, Page page)
    {
        Entity = entity;
        Page = page;
        Lists.Push(new ActionList(page.Actions));
    }

    #endregion

    #region Properties

    public MapEntity Entity { get; }

    public Page Page { get; }

    public Stack<ActionList> Lists { get; } = new();

    // the action the page is waiting on, its handler is asked every update until it's done
    public PageAction? Busy { get; set; }

    // everything but a parallel process stops the player until it's done, except a page waiting on a battle: the
    // player fights it
    public bool BlocksPlayer => Page.PageTrigger is not PageTrigger.ParallelProcess && !WaitsForBattle;

    public bool IsFinished => Lists.Count == 0 && Busy is null;

    // the page transferred the player, it carries on running on the new map
    public bool CarriesOver { get; set; }

    // the page started a battle and waits for it to end: it lasts through the battle map and back, and carries on
    // on the story map afterwards (to erase the enemy, for example)
    public bool WaitsForBattle { get; set; }

    #endregion

    #region Methods

    // what a handler keeps between updates while this page runs, made the first time it asks
    public T State<T>() where T : class, new()
    {
        if (_states.TryGetValue(typeof(T), out var state)) return (T) state;

        var created = new T();
        _states[typeof(T)] = created;
        return created;
    }

    #endregion
}

/// <summary>
/// A list of actions and the next one to run.
/// </summary>
public class ActionList(IList<PageAction> actions)
{
    public IList<PageAction> Actions => actions;

    public int Index { get; set; }
}
