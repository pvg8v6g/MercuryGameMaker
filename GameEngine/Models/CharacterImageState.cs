namespace GameEngine.Models;

/// <summary>
/// The character picture drawn for an entity and where its walk animation is.
/// </summary>
public class CharacterImageState
{
    #region Properties

    public SceneSprite? Sprite { get; set; }

    // the character sheet the sprite was made from, a page change can swap it
    public string? ImagePath { get; set; }

    // the column picked in the editor, a new pick makes a new sprite
    public int Index { get; set; }

    // a walking sheet holds characters of 3 frames by 4 directions, this is the picked character's top left cell
    public bool Walks { get; set; }

    public int Column { get; set; }

    public int Row { get; set; }

    // the walk frame inside the character: 0 and 2 are steps, 1 is standing
    public int Frame { get; set; }

    public int SteppingIndex { get; set; }

    public int SteppingTimer { get; set; }

    public int NotMovingTimer { get; set; }

    #endregion
}
