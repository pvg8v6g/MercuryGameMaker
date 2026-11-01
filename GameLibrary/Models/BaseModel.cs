using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models;

public partial class BaseModel : PropertyChangedUpdater
{
    #region Properties

    public Guid Guid
    {
        get;
        set => SetField(ref field, value);
    } = Guid.NewGuid();

    // reads the "uuid" saved by the java editor, never written
    [JsonInclude]
    private Guid Uuid
    {
        set => Guid = value;
    }

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

    public int Icon
    {
        get;
        set => SetField(ref field, value);
    } = 0;

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
