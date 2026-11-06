using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// An area drawn on the map. The area stores pixels, the editor works in cells of the area grid
/// (48 pixels is a tile, smaller cells make finer collisions).
/// </summary>
public partial class MapAreaModel(MapArea area, int cellSize) : PropertyChangedUpdater
{
    #region Brushes

    private static readonly Brush AreaBrush = new SolidColorBrush(ColorHelper.FromArgb(90, 120, 190, 255));
    private static readonly Brush CollisionBrush = new SolidColorBrush(ColorHelper.FromArgb(110, 230, 60, 60));
    private static readonly Brush WaterBrush = new SolidColorBrush(ColorHelper.FromArgb(110, 40, 90, 220));
    private static readonly Brush FootstepBrush = new SolidColorBrush(ColorHelper.FromArgb(110, 80, 200, 110));

    #endregion

    #region Properties

    public MapArea Area => area;

    public double X => area.OffsetX;

    public double Y => area.OffsetY;

    public double Width => area.Width;

    public double Height => area.Height;

    public string Name
    {
        get => area.Name;
        set
        {
            if (area.Name == value) return;
            area.Name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
        }
    }

    public MapAreaType AreaType
    {
        get => area.AreaType;
        set
        {
            if (area.AreaType == value) return;
            area.AreaType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Fill));
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(IsFootstep));
        }
    }

    public bool IsFootstep => area.AreaType == MapAreaType.Footstep;

    public string Label => area.AreaType == MapAreaType.Area && !string.IsNullOrEmpty(area.Name) ? area.Name : area.AreaType.ToString();

    public Brush Fill => area.AreaType switch
    {
        MapAreaType.Collision => CollisionBrush,
        MapAreaType.Water => WaterBrush,
        MapAreaType.Footstep => FootstepBrush,
        _ => AreaBrush
    };

    // the size of one cell in pixels, follows the area grid picked in the editor
    public int CellSize
    {
        get => cellSize;
        set
        {
            if (value <= 0 || value == cellSize) return;
            cellSize = value;
            OnPropertyChanged();
            OnBoundsChanged();
        }
    }

    public int CellX
    {
        get => area.OffsetX / cellSize;
        set
        {
            area.OffsetX = Math.Max(0, value) * cellSize;
            OnBoundsChanged();
        }
    }

    public int CellY
    {
        get => area.OffsetY / cellSize;
        set
        {
            area.OffsetY = Math.Max(0, value) * cellSize;
            OnBoundsChanged();
        }
    }

    public int CellWidth
    {
        get => area.Width / cellSize;
        set
        {
            area.Width = Math.Max(1, value) * cellSize;
            OnBoundsChanged();
        }
    }

    public int CellHeight
    {
        get => area.Height / cellSize;
        set
        {
            area.Height = Math.Max(1, value) * cellSize;
            OnBoundsChanged();
        }
    }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    public bool Contains(double worldX, double worldY)
    {
        return worldX >= X && worldX < X + Width && worldY >= Y && worldY < Y + Height;
    }

    public void MoveTo(int x, int y)
    {
        area.OffsetX = x;
        area.OffsetY = y;
        OnBoundsChanged();
    }

    #endregion

    #region Private Methods

    private void OnBoundsChanged()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(CellX));
        OnPropertyChanged(nameof(CellY));
        OnPropertyChanged(nameof(CellWidth));
        OnPropertyChanged(nameof(CellHeight));
    }

    #endregion
}
