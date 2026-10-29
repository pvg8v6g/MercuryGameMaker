using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Actions;

public partial class Choice : PropertyChangedUpdater
{
    /// <summary>The text displayed for this choice.</summary>
    public string Text
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public override string ToString() => Text;
}
