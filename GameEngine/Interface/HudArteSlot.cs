using Windows.Foundation;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Media;

namespace GameEngine.Interface;

/// <summary>
/// One arte key on the battle HUD: the arte's icon, its key, its Mana cost, and a clock wipe over the icon while it
/// cools down (a dark sweep that covers the whole icon when it's used and wipes away clockwise as it gets ready).
/// Made and changed on the ui thread only.
/// </summary>
public partial class HudArteSlot(string key) : PropertyChangedUpdater
{
    #region Constants

    // the icon box, in screen pixels
    public const double Size = 40;

    #endregion

    #region Properties

    public string Key => key;

    public bool HasArte
    {
        get;
        set => SetField(ref field, value);
    }

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public string CostText
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    // the icon, cut out of its sheet: Icon is the whole sheet, IconRect the cell
    public ImageSource? Icon
    {
        get;
        set => SetField(ref field, value);
    }

    public Rect IconRect
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(IconClip));
            OnPropertyChanged(nameof(IconLeft));
            OnPropertyChanged(nameof(IconTop));
            OnPropertyChanged(nameof(IconWidth));
            OnPropertyChanged(nameof(IconHeight));
        }
    }

    public Rect IconClip => new(0, 0, IconRect.Width, IconRect.Height);

    public double IconLeft => -IconRect.X;

    public double IconTop => -IconRect.Y;

    public double IconWidth => Math.Max(1, IconRect.Width);

    public double IconHeight => Math.Max(1, IconRect.Height);

    // cooling down: the icon is greyed and the wipe covers what's left of the cooldown
    public bool IsCooling { get; private set; }

    public double IconOpacity => IsCooling ? 0.45 : 1.0;

    public Geometry? Wipe { get; private set; }

    // not enough Mana to use it
    public bool NoMana
    {
        get;
        private set => SetField(ref field, value);
    }

    // the arte being cast right now
    public bool IsCasting
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Fields

    private double _shownFraction = -1;

    #endregion

    #region Methods

    // fraction is how much of the cooldown is left, 1 just used and 0 ready
    public void Set(double fraction, bool noMana, bool casting)
    {
        NoMana = noMana;
        IsCasting = casting;

        var cooling = fraction > 0;
        if (cooling != IsCooling)
        {
            IsCooling = cooling;
            OnPropertyChanged(nameof(IsCooling));
            OnPropertyChanged(nameof(IconOpacity));
        }

        // the wipe only needs drawing again when it has visibly moved
        if (Math.Abs(fraction - _shownFraction) < 0.005) return;

        _shownFraction = fraction;
        Wipe = cooling ? ClockWipe(fraction) : null;
        OnPropertyChanged(nameof(Wipe));
    }

    #endregion

    #region Private Methods

    // the part of the icon still covered: from the edge that has swept round so far, clockwise back up to the top.
    // The circle is bigger than the box so it covers the corners too, the box clips it
    private static Geometry ClockWipe(double fraction)
    {
        var center = new Point(Size / 2, Size / 2);
        var radius = Size;
        if (fraction >= 0.999) return new RectangleGeometry { Rect = new Rect(0, 0, Size, Size) };

        var start = (1 - fraction) * 360;
        var figure = new PathFigure { StartPoint = center, IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment { Point = OnCircle(center, radius, start) });
        figure.Segments.Add(new ArcSegment
        {
            Point = OnCircle(center, radius, 359.999),
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = fraction > 0.5
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    // degrees clockwise from the top
    private static Point OnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + (radius * Math.Sin(radians)), center.Y - (radius * Math.Cos(radians)));
    }

    #endregion
}
