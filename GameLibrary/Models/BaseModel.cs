using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models;

public class BaseModel : PropertyChangedUpdater
{
    #region Properties

    [JsonIgnore]
    public Guid Guid { get; } = Guid.NewGuid();

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
