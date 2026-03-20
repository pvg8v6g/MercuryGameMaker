using GameLibrary.Enumerations;

namespace GameLibrary.Models.Items;

public partial class Item : BaseModel
{
    #region Properties

    public string? Description
    {
        get;
        set => SetField(ref field, value);
    }

    public int Cost
    {
        get;
        set => SetField(ref field, value);
    }

    public ItemSort Sort
    {
        get;
        set => SetField(ref field, value);
    } = ItemSort.All;

    #endregion
}
