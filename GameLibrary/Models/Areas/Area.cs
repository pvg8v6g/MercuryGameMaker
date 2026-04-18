using System.Text.Json.Serialization;

namespace GameLibrary.Models.Areas;

public partial class Area : BaseModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the current world X coordinate, updated dynamically during character movement. This value is not persisted.
    /// </summary>
    [JsonIgnore]
    public int X
    {
        get => field + OffsetX;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Gets or sets the current world Y coordinate, updated dynamically during character movement. This value is not persisted.
    /// </summary>
    [JsonIgnore]
    public int Y
    {
        get => field + OffsetY;
        set => SetField(ref field, value);
    }

    /// <summary>
    /// Gets or sets the horizontal displacement used to position the hitbox relative to the sprite.
    /// </summary>
    public int OffsetX
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(X));
        }
    }

    /// <summary>
    /// Gets or sets the vertical displacement used to position the hitbox relative to the sprite.
    /// </summary>
    public int OffsetY
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(Y));
        }
    }

    public int Width
    {
        get;
        set => SetField(ref field, value);
    }

    public int Height
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    public override string ToString()
    {
        return $"{X}, {Y}, {Width}, {Height}";
    }
}
