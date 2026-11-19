using GameEngine.Models;
using GameEngine.Services.GameState;
using GameEngine.Services.Input;
using GameEngine.Systems;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages;

/// <summary>
/// Starts entity pages by their trigger and runs them. An entity runs one page at a time.
/// Action: the player presses enter facing the entity. Player touch: the player walks into it.
/// Event touch: either one walks into the other. Parallel process: runs over and over while the page is active,
/// next to everything else. Autorun: the same, but nothing else runs and the player can't move until it's done.
/// A page the player starts stops the player until it's done, parallel processes keep going.
/// </summary>
public class PageRunner(
    IGameDataService gameDataService,
    IGameStateService gameStateService,
    IInputService inputService,
    HitboxSystem hitboxSystem,
    PageTouches pageTouches,
    ActionRunner actionRunner)
{
    #region Fields

    private readonly Dictionary<MapEntity, PageRun> _runs = [];
    private bool _enterHeld;

    #endregion

    #region Methods

    // a new map starts with nothing running
    public void Clear()
    {
        _runs.Clear();
        pageTouches.Take();
        gameStateService.IsPlayerBusy = false;
    }

    // called once an update after the systems, when the map isn't paused
    public void Update()
    {
        // enter starts a page when it goes down, holding it (after closing a page with it) doesn't start another
        var enterPressed = inputService.Enter && !_enterHeld;
        _enterHeld = inputService.Enter;

        StartRepeating();
        if (_runs.Values.Any(x => x.BlocksPlayer)) gameStateService.IsPlayerBusy = true;
        var touched = pageTouches.Take();
        if (!gameStateService.IsPlayerBusy)
        {
            if (enterPressed && Facing() is { } faced) Start(faced, PageTrigger.Action);
            foreach (var entity in touched) Start(entity, PageTrigger.PlayerTouch, PageTrigger.EventTouch);
        }

        // an autorun holds everything else, the first one runs until it's done
        var autorun = _runs.Values.FirstOrDefault(x => x.Page.PageTrigger == PageTrigger.Autorun);
        foreach (var run in autorun is not null ? [autorun] : _runs.Values.ToArray()) actionRunner.Update(run);

        foreach (var finished in _runs.Where(x => x.Value.IsFinished).Select(x => x.Key).ToArray()) _runs.Remove(finished);
        gameStateService.IsPlayerBusy = _runs.Values.Any(x => x.BlocksPlayer);
        gameStateService.MapEntities.RemoveAll(x => x.IsErased);
    }

    #endregion

    #region Private Methods

    // parallel processes and autoruns start again as soon as they finish, for as long as their page is active
    private void StartRepeating()
    {
        foreach (var entity in gameStateService.MapEntities)
        {
            if (entity.ActivePage is { PageTrigger: PageTrigger.ParallelProcess or PageTrigger.Autorun } page && !_runs.ContainsKey(entity))
            {
                _runs[entity] = new PageRun(entity, page);
            }
        }
    }

    // starts the entity's active page when its trigger is one of these, unless the entity is already running one.
    // The player starts one page at a time
    private void Start(MapEntity entity, params PageTrigger[] triggers)
    {
        if (gameStateService.IsPlayerBusy || entity.IsErased || _runs.ContainsKey(entity) || entity.ActivePage is not { } page || !triggers.Contains(page.PageTrigger)) return;

        // every entity turns to the player when they start its page, unless its direction is fixed
        FacePlayer(entity);
        _runs[entity] = new PageRun(entity, page);
        gameStateService.IsPlayerBusy = true;
    }

    // the entity in front of the player on the player's level, or under the player when nothing is in front.
    // In front is half a tile past the player's feet the way the player faces
    private MapEntity? Facing()
    {
        if (gameStateService.Player is not { } player || player.GetModule<PositionModule>() is not { } position) return null;
        if (player.State<MovementState>().IsMoving || hitboxSystem.Main(player) is not { } feet) return null;

        var reach = gameDataService.GridSize * gameDataService.ScaleFactor / 2.0;
        var (x, y) = position.Direction switch
        {
            Direction.Up => (0.0, -reach),
            Direction.Down => (0.0, reach),
            Direction.Left => (-reach, 0.0),
            Direction.Right => (reach, 0.0),
            _ => (0.0, 0.0)
        };

        var level = HitboxSystem.Level(position);
        var others = gameStateService.MapEntities
            .Where(entity => entity != player && !entity.IsErased && entity.ActivePage?.PageTrigger == PageTrigger.Action)
            .Where(entity => entity.GetModule<PositionModule>() is { } other && HitboxSystem.Level(other) == level)
            .ToArray();

        return others.FirstOrDefault(entity => hitboxSystem.PlacedOf(entity).Any(box => box.Collide(feet.X + x, feet.Y + y, feet.Width, feet.Height)))
               ?? others.FirstOrDefault(entity => hitboxSystem.PlacedOf(entity).Any(feet.Collide));
    }

    // turns toward the player along whichever way the player is farther, an entity with a fixed direction stays as it is
    private void FacePlayer(MapEntity entity)
    {
        if (entity.GetModule<MovementModule>() is { FixDirection: true }) return;
        if (entity.GetModule<PositionModule>() is not { } position || gameStateService.Player?.GetModule<PositionModule>() is not { } player) return;

        var dx = player.X - position.X;
        var dy = player.Y - position.Y;
        if (dx == 0 && dy == 0) return;

        position.Direction = Math.Abs(dx) > Math.Abs(dy)
            ? dx > 0 ? Direction.Right : Direction.Left
            : dy > 0 ? Direction.Down : Direction.Up;
    }

    #endregion
}
