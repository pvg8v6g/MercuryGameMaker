using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models;

public class BaseModel : PropertyChangedUpdater
{
    #region Properties

    public Guid Guid
    {
        get;
        set => SetField(ref field, value);
    } = Guid.NewGuid();

    public int Id
    {
        get;
        set => SetField(ref field, value);
    }

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public int? Icon
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Overrides

    public override bool Equals(object? obj)
    {
        return obj is BaseModel baseModel && baseModel.Guid.Equals(Guid);
    }

    public override int GetHashCode()
    {
        return 0;
    }

    #endregion
}
