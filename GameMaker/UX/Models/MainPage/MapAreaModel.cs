using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// An area drawn on the map. The area stores pixels, the editor works in tiles.
/// </summary>
public partial class MapAreaModel(MapArea area, int tileSize) : PropertyChangedUpdater
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

    public int TileX
    {
        get => area.OffsetX / tileSize;
        set
        {
            area.OffsetX = Math.Max(0, value) * tileSize;
            OnBoundsChanged();
        }
    }

    public int TileY
    {
        get => area.OffsetY / tileSize;
        set
        {
            area.OffsetY = Math.Max(0, value) * tileSize;
            OnBoundsChanged();
        }
    }

    public int TileWidth
    {
        get => area.Width / tileSize;
        set
        {
            area.Width = Math.Max(1, value) * tileSize;
            OnBoundsChanged();
        }
    }

    public int TileHeight
    {
        get => area.Height / tileSize;
        set
        {
            area.Height = Math.Max(1, value) * tileSize;
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
        OnPropertyChanged(nameof(TileX));
        OnPropertyChanged(nameof(TileY));
        OnPropertyChanged(nameof(TileWidth));
        OnPropertyChanged(nameof(TileHeight));
    }

    #endregion
}
