namespace GameLibrary.Models.Areas;

public partial class Area : BaseModel
{
    #region Properties

    public int X
    {
        get;
        set => SetField(ref field, value);
    }

    public int Y
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
}
