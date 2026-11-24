namespace GameLibrary.Enumerations;

/// <summary>
/// What a lost battle leads to once the Game Over screen is closed (GameSettings.Defeat). A battle whose troop is marked Can Lose never
/// gets here: the story simply carries on.
/// </summary>
public enum DefeatOutcome
{
    // the game is over: back to the title or the last save
    GameOver,

    // the same battle is fought again from the start
    Retry
}
