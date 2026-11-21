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

    // the icon and the sheet it's on, GameIcon.None for no icon
    public GameIcon Icon
    {
        get;
        set => SetField(ref field, value ?? GameIcon.None);
    } = GameIcon.None;

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
