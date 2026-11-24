using GameEngine.Services.Input;
using GameLibrary.Enumerations;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;

namespace GameEngine.Interface;

/// <summary>
/// The screen layer shown when the party is defeated in a battle it was allowed to lose nothing in. It stops the game,
/// plays the game's defeat sound effect (chosen on the Terms page) and says what happens next, which is the game's
/// defeat setting: the game is over, or the battle is fought again. Enter or Space closes it, and the battle is settled
/// behind it.
/// </summary>
public partial class GameOverScreen(
    IGameDataService gameDataService,
    IInputService inputService,
    IAudioService audioService) : PromptScreen(inputService)
{
    #region Properties

    // what pressing Enter does, set on the ui thread when the screen opens
    public string PromptText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    #endregion

    #region Methods

    // called from the game loop once the party has been down for the short pause
    public void Show(DefeatOutcome outcome)
    {
        Begin();

        if (gameDataService.GameSettings.DefeatSfx is { Name: { Length: > 0 } } sfx) audioService.PlaySfx(sfx.Name, sfx.Volume, sfx.Pitch);

        var prompt = outcome == DefeatOutcome.Retry ? "Press Enter to try again" : "Press Enter to continue";
        Dispatcher.TryEnqueue(() =>
        {
            PromptText = prompt;
            Reveal();
        });
    }

    #endregion

    #region Protected Methods

    // the sound doesn't carry on after the screen is closed
    protected override void Close()
    {
        audioService.StopSfx();
        base.Close();
    }

    #endregion
}
