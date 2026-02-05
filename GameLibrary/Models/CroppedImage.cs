using Windows.Foundation;

namespace GameLibrary.Models;

public class CroppedImage
{
    public string? ImageSource { get; set; } = null;

    public Rect? Rect { get; set; } = null;
}
