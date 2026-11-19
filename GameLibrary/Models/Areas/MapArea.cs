using GameLibrary.Enumerations;
using GameLibrary.Models.Media;

namespace GameLibrary.Models.Areas;

/// <summary>
/// A rectangle on a map. OffsetX/OffsetY are the pixel position on the map.
/// </summary>
public partial class MapArea : Area
{
    #region Properties

    public MapAreaType AreaType
    {
        get;
        set => SetField(ref field, value);
    }

    // the color the area is drawn in (editor and test overlay), none is its type's color
    public GameColor? Color
    {
        get;
        set => SetField(ref field, value);
    }

    // the sound played when walking inside a footstep area
    public AudioFile? Footstep
    {
        get;
        set => SetField(ref field, value);
    }

    // the directions a collision area stops entities moving onto it in, all four is a solid wall
    public bool BlocksUp
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool BlocksDown
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool BlocksLeft
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool BlocksRight
    {
        get;
        set => SetField(ref field, value);
    } = true;

    // a heat haze area: how far it pulls the ground (pixels), the size of one ripple and how fast it rises (pixels a second)
    public double HazeStrength
    {
        get;
        set => SetField(ref field, value);
    } = 3;

    public double HazeRippleSize
    {
        get;
        set => SetField(ref field, value);
    } = 32;

    public double HazeSpeed
    {
        get;
        set => SetField(ref field, value);
    } = 40;

    #endregion

    #region Methods

    // the ground of the area's level: a raised platform's top or a bridge deck. Collisions and level changes only
    // say where you can walk and how you get there, heat haze is only seen, any other area is somewhere you stand
    public bool IsSurface => AreaType is not (MapAreaType.Collision or MapAreaType.LevelChange or MapAreaType.HeatHaze);

    // whether moving this way onto the area is stopped
    public bool Blocks(Direction direction) => direction switch
    {
        Direction.Up => BlocksUp,
        Direction.Down => BlocksDown,
        Direction.Left => BlocksLeft,
        Direction.Right => BlocksRight,
        _ => false
    };

    #endregion
}
