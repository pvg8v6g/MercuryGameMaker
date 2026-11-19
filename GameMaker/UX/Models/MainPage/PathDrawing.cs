using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// A line between two points of a patrol path, in map pixels. The picked path is drawn brighter than the others.
/// </summary>
public class PathSegment(double x1, double y1, double x2, double y2, bool picked)
{
    private static readonly Brush PickedBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 170, 40));
    private static readonly Brush OtherBrush = new SolidColorBrush(ColorHelper.FromArgb(150, 200, 200, 200));

    public double X1 => x1;

    public double Y1 => y1;

    public double X2 => x2;

    public double Y2 => y2;

    public Brush Stroke => picked ? PickedBrush : OtherBrush;
}

/// <summary>
/// A numbered dot on a patrol point, X/Y is its middle in map pixels. The picked point is ringed, the path's name shows
/// next to its first point.
/// </summary>
public class PathMarker(double x, double y, string number, bool picked, bool selected, string? name)
{
    private const double Size = 22;

    private static readonly Brush PickedBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 170, 40));
    private static readonly Brush OtherBrush = new SolidColorBrush(ColorHelper.FromArgb(200, 120, 120, 120));
    private static readonly Brush RingBrush = new SolidColorBrush(Colors.White);
    private static readonly Brush NoRingBrush = new SolidColorBrush(Colors.Transparent);

    public double Left => x - (Size / 2);

    public double Top => y - (Size / 2);

    public double Diameter => Size;

    public string Number => number;

    public string Name => name ?? string.Empty;

    public bool HasName => !string.IsNullOrEmpty(name);

    public Brush Fill => picked ? PickedBrush : OtherBrush;

    public Brush Ring => selected ? RingBrush : NoRingBrush;
}
