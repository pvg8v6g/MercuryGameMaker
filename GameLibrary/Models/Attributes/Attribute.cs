namespace GameLibrary.Models.Attributes;

public partial class Attribute : BaseModel
{
    #region Properties

    public bool IsMagicBased
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
