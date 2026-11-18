namespace GameLibrary.Enumerations;

public enum PageTrigger
{
    Action, // player presses the action button while facing the entity
    PlayerTouch, // player simply touches the entity
    EventTouch, // player touches the entity OR entity touches player
    ParallelProcess, // runs the page immediately upon map load (other entity pages can also run in parallel)
    Autorun // runs the page immediately upon map load (other entity pages are blocked until completion)
}
