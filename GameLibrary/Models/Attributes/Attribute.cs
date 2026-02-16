namespace GameLibrary.Models.Attributes;

public class Attribute : BaseModel
{
    #region Properties

    public bool IsMagicBased
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
