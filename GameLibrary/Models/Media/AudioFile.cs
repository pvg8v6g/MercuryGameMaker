using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Media;

public partial class AudioFile : PropertyChangedUpdater
{
    #region Properties

    public string? Name
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Path
    {
        get;
        set => SetField(ref field, value);
    } = "BGM";

    public double Volume
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double Pitch
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    #endregion
}
