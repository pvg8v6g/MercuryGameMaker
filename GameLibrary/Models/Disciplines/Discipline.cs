namespace GameLibrary.Models.Disciplines;

public partial class Discipline : BaseModel
{
    #region Properties

    public Guid LifeGrowthGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid ManaGrowthGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public Dictionary<Guid, Guid> AttributeGrowths
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public string Description
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    #endregion
}
