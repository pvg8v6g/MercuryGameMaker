using Windows.Foundation;

namespace GameLibrary.Models;

public class TopBarModel
{
    public CroppedImage? CroppedImage { get; set; }

    public bool IsChecked { get; set; }

    public string CommandIndex { get; set; } = "0";

    public string Tooltip { get; set; } = string.Empty;
}
