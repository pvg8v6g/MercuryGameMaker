namespace GameLibrary.Models.Fighter;

public partial class DropModel : BaseModel
{
    public Guid DropId
    {
        get;
        set => SetField(ref field, value);
    }

    public int DropPercent
    {
        get;
        set => SetField(ref field, value);
    }
}
