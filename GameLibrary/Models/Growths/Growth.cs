namespace GameLibrary.Models.Growths;

public partial class Growth : BaseModel
{
    #region Properties

    public Dictionary<int, int> GrowthValues { get; set; } = new(); // first integer: level; second: value

    #endregion
}
