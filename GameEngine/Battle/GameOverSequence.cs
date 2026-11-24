namespace GameEngine.Battle;

/// <summary>
/// What happens once the Game Over screen is closed and the game's defeat setting is Game Over. The one place that will
/// grow into the real thing when the title screen and saves are built.
/// </summary>
public class GameOverSequence
{
    #region Methods

    /// <summary>
    /// The game is over. <paramref name="returnToMap"/> ends the battle and puts the party back on the story map.
    /// </summary>
    public void Play(Action returnToMap)
    {
        // TODO: there is no title screen or saved game yet. Once there are, this is where the game goes back to the title
        // or to the last save, and the party only returns to the map for the time being
        returnToMap();
    }

    #endregion
}
