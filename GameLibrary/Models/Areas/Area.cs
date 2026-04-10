namespace GameLibrary.Models.Areas;

public partial class Area : BaseModel
{
    #region Properties

    public int X
    {
        get => field + OffsetX;
        set => SetField(ref field, value);
    }

    public int Y
    {
        get => field + OffsetY;
        set => SetField(ref field, value);
    }

    public int OffsetX
    {
        get;
        set => SetField(ref field, value);
    }

    public int OffsetY
    {
        get;
        set => SetField(ref field, value);
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
