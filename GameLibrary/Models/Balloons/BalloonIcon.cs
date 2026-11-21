namespace GameLibrary.Models.Balloons;

/// <summary>
/// A balloon icon shown over an entity's head (a heart, a "!", a "..."). Row is its row on the balloon sheet
/// (Graphics\Balloons\~Balloons.png). The row's cells are the frames: the balloon plays them left to right once, then goes away.
/// </summary>
public partial class BalloonIcon : BaseModel
{
    #region Constants

    // every cell on the balloon sheet is one tile, in unscaled map pixels
    public const int CellSize = 48;

    #endregion

    #region Properties

    // the row on the balloon sheet, saved in Icon (as its index) so older files still load
    [System.Text.Json.Serialization.JsonIgnore]
    public int Row
    {
        get => Icon.Index;
        set
        {
            if (value == Row) return;
            Icon = new GameIcon(null, value);
            OnPropertyChanged();
        }
    }

    // milliseconds each frame of the row is shown
    public int FrameTime
    {
        get;
        set => SetField(ref field, value);
    } = 100;

    // milliseconds the last frame stays up before the balloon goes away
    public int HoldTime
    {
        get;
        set => SetField(ref field, value);
    } = 400;

    #endregion
}
