using GameLibrary.Models.Balloons;

namespace GameEngine.Models;

/// <summary>
/// The balloon playing over an entity's head (a "!" when it spots the player, a heart from a page), where it is in
/// its row and the picture drawn for it. No balloon: nothing is drawn.
/// </summary>
public class BalloonState
{
    #region Properties

    public BalloonIcon? Balloon { get; set; }

    // the column of the balloon's row being shown, frames run left to right
    public int Frame { get; set; }

    // updates the current frame has been shown
    public int Timer { get; set; }

    public SceneSprite? Sprite { get; set; }

    #endregion
}
