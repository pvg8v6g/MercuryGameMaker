using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Props;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// A prop placed on the map in the editor. The map stores the base point (bottom middle of the picture),
/// the editor also needs the picture's box and its footprint to draw, select and move it.
/// </summary>
public partial class MapPropModel(MapProp placed, Prop prop, string imagePath, double width, double height) : PropertyChangedUpdater
{
    #region Properties

    public MapProp Placed => placed;

    public Prop Prop => prop;

    // the picture drawn, the mirrored copy when the prop is flipped
    public string ImagePath
    {
        get;
        set => SetField(ref field, value);
    } = imagePath;

    public double ImageWidth => width;

    public double ImageHeight => height;

    // top left of the picture in map pixels
    public double X => placed.X - width / 2.0;

    public double Y => placed.Y - height;

    // the line characters sort against, the bottom of the picture plus the prop's sort offset
    public double SortBottom => placed.Y + prop.SortOffset;

    // the footprint boxes in map pixels, mirrored with the picture
    public ObservableCollection<Rect> Footprint { get; } = [];

    public PictureBox? Picture { get; set; }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
    }

    // the level is worked out from the surface under the prop's base unless it's set by hand (a crate under a bridge)
    public bool HasLevel
    {
        get => placed.Level is not null;
        set
        {
            if (value == HasLevel) return;
            placed.Level = value ? 0 : null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Level));
        }
    }

    public int Level
    {
        get => placed.Level ?? 0;
        set
        {
            if (placed.Level is null || placed.Level == value) return;
            placed.Level = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Methods

    public bool Contains(double worldX, double worldY)
    {
        return worldX >= X && worldX < X + width && worldY >= Y && worldY < Y + height;
    }

    public void MoveTo(int baseX, int baseY)
    {
        placed.X = baseX;
        placed.Y = baseY;
        Refresh();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(SortBottom));
        Footprint.Clear();
        foreach (var area in prop.Footprint)
        {
            var offsetX = placed.Flip ? width - area.OffsetX - area.Width : area.OffsetX;
            Footprint.Add(new Rect(X + offsetX, Y + area.OffsetY, area.Width, area.Height));
        }
    }

    #endregion
}
