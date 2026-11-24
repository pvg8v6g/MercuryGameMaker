using VirtualKey = Windows.System.VirtualKey;
using GameEngine.Scenes;
using GameEngine.Services.Input;
using Microsoft.UI.Dispatching;

namespace GameEngine.Interface;

/// <summary>
/// A screen layer that stops the game and waits for the player to press Enter or Space, like the victory and game over
/// screens. The battle waits on <see cref="IsActive"/>, which is true from the moment the screen is started until it's
/// closed. The game loop starts it (<see cref="Begin"/>) and the ui thread shows it (<see cref="Reveal"/>), and a fresh press
/// of Enter or Space closes it once it's been up a moment.
/// </summary>
public abstract partial class PromptScreen(IInputService inputService) : ScreenLayer
{
    #region Constants

    // updates before a key can close it, so a key held down from the fight doesn't skip it unseen
    private const int MinimumShown = 45;

    #endregion

    #region Fields

    private volatile bool _active;
    private volatile bool _ready;
    private int _age;
    private bool _wasDown;

    #endregion

    #region Properties

    public override bool PausesGame => true;

    // the battle is waiting on this: true from the moment it's started until it's closed
    public bool IsActive => _active;

    protected DispatcherQueue Dispatcher { get; } = DispatcherQueue.GetForCurrentThread();

    #endregion

    #region Methods

    public override void Update(GameCamera camera)
    {
        if (!_active || !_ready) return;

        // a fresh press of Enter or Space, after it's been up a moment
        var down = inputService.Enter || inputService.IsDown(VirtualKey.Space);
        var pressed = down && !_wasDown;
        _wasDown = down;
        if (++_age < MinimumShown || !(pressed || ClosesItself())) return;

        _active = false;
        _ready = false;
        Dispatcher.TryEnqueue(Close);
    }

    #endregion

    #region Protected Methods

    // from the game loop: the screen counts as active at once, before anything is on screen to wait for
    protected void Begin()
    {
        _age = 0;
        _wasDown = false;
        _ready = false;
        _active = true;
    }

    // on the ui thread, once everything the screen shows is ready: it opens (and pauses the game) and can be closed
    protected void Reveal()
    {
        IsOpen = true;
        _ready = true;
    }

    // true once the screen has nothing left to wait for and should go away without a key press (never, unless a screen
    // says so). Enter or Space still closes it early. Called from the game loop
    protected virtual bool ClosesItself() => false;

    // on the ui thread
    protected virtual void Close()
    {
        IsOpen = false;
    }

    #endregion
}
