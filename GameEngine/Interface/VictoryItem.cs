using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameEngine.Interface;

/// <summary>
/// One line of the spoils on the victory screen: the money or an item picked up, with its icon cut out of its sheet.
/// Made on the ui thread and never changed.
/// </summary>
public class VictoryItem(string name, int count, (string path, Rect cell)? icon)
{
    public string Name => name;

    // blank for money, the amount is already in its name
    public string CountText => count > 1 ? $"×{count}" : string.Empty;

    public ImageSource? Icon { get; } = icon is { } found ? new BitmapImage(new Uri(found.path)) : null;

    public Rect IconRect => icon?.cell ?? default;

    public Rect IconClip => new(0, 0, IconWidth, IconHeight);

    public double IconLeft => -IconRect.X;

    public double IconTop => -IconRect.Y;

    public double IconWidth => Math.Max(1, IconRect.Width);

    public double IconHeight => Math.Max(1, IconRect.Height);
}
