using GameLibrary.Models;

namespace GameMaker.UX.Components.EngineRadioIcon;

public class EngineRadioIconModel
{
    public CroppedImage? CroppedImage { get; set; }

    public bool IsChecked { get; set; }

    public string CommandIndex { get; set; } = "0";

    public string Tooltip { get; set; } = string.Empty;
}
