using GameLibrary.Models.Animations;
using GameLibrary.Models.Artes;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Models;

/// <summary>
/// The animations playing on an entity.
/// </summary>
public class AnimationState
{
    public List<PlayingAnimation> Playing { get; } = [];
}

/// <summary>
/// One animation playing: the frame it's on (0 before the first update shows frame 1) and a sprite per cell, kept
/// from frame to frame so the canvas doesn't reload a picture every update.
/// </summary>
public class PlayingAnimation(Animation animation, int lastFrame)
{
    #region Fields

    private readonly Dictionary<int, SceneSprite> _cells = [];
    private readonly Dictionary<object, SceneSprite> _distortions = [];

    #endregion

    #region Properties

    public Animation Animation => animation;

    public int LastFrame => lastFrame;

    public int Frame { get; set; }

    // mirrors, turns and moves the whole animation about the middle of its target: an arte that moves and rotates one
    // animation for each way its user faces. None plays it as drawn
    public ArteAnimationPlacement? Placement { get; init; }

    // its sheets are still being measured, it starts when they are
    public bool Waiting { get; set; }

    // the last tint it put on its target, faded out when it ends if it's still on
    public AnimationTint? LastTint { get; set; }

    // makes every play of the animation a different bolt of lightning
    public int Seed { get; } = Random.Shared.Next();

    // the cells on the frame showing now
    public List<SceneSprite> Showing { get; } = [];

    #endregion

    #region Methods

    // the cell's sprite, a new one when the cell first shows or moves to another sheet
    public SceneSprite Cell(int cell, string path, int z)
    {
        if (_cells.TryGetValue(cell, out var sprite) && sprite.Picture.ImagePath == path) return sprite;

        sprite = new SceneSprite(new PictureBox { ImagePath = path, Z = z });
        _cells[cell] = sprite;
        return sprite;
    }

    // the shockwave's, heat's or lightning's sprite, kept for as long as it goes on. It has no picture to size, so the renderer
    // puts it exactly where it's placed
    public SceneSprite Distortion(object source, Func<PictureBox> create)
    {
        if (_distortions.TryGetValue(source, out var sprite)) return sprite;

        sprite = new SceneSprite(create());
        _distortions[source] = sprite;
        return sprite;
    }

    #endregion
}
