using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.MainPage.Tools;

/// <summary>
/// Areas mode: click or drag on empty cells to draw an area, drag an area to move it. Everything stays on the
/// area grid (48 is a tile, smaller makes finer collisions).
/// </summary>
public partial class AreaTool(MainPageViewModel editor) : PropertyChangedUpdater, IMapTool
{
    #region Fields

    private MapAreaModel? _drag;
    private Point _dragOffset;
    private Point? _drawStart;

    #endregion

    #region Properties

    public MapAreaType[] AreaTypes { get; } = Enum.GetValues<MapAreaType>();

    // every size divides the 48 pixel tile
    public int[] GridSizes { get; } = [48, 24, 16, 12, 8, 6, 4];

    public int GridSize
    {
        get;
        set
        {
            if (value <= 0 || field == value) return;
            SetField(ref field, value);
            foreach (var area in editor.Areas) area.CellSize = value;
            editor.OnSnapSizeChanged();
        }
    } = 48;

    // the type given to newly drawn areas
    public MapAreaType NewAreaType
    {
        get;
        set => SetField(ref field, value);
    } = MapAreaType.Collision;

    public int SnapSize => GridSize;

    public bool CanPaste => false;

    public Rect DrawPreview
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool IsDrawing
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Input

    public void Pressed(Point world, bool leftButton)
    {
        var cell = Cell(world);
        var area = editor.Areas.LastOrDefault(x => x.Contains(world.X, world.Y));
        editor.Select(area);
        if (!leftButton) return;

        if (area is not null)
        {
            _drag = area;
            _dragOffset = new Point(cell.X - area.X, cell.Y - area.Y);
            return;
        }

        _drawStart = cell;
        UpdateDrawPreview(cell);
    }

    public void Moved(Point world, bool inside)
    {
        var cell = Cell(world);
        if (_drag is not null)
        {
            var x = Math.Clamp(cell.X - _dragOffset.X, 0, Math.Max(0, editor.MapPixelWidth - _drag.Width));
            var y = Math.Clamp(cell.Y - _dragOffset.Y, 0, Math.Max(0, editor.MapPixelHeight - _drag.Height));
            _drag.MoveTo((int) x, (int) y);
        }
        else if (_drawStart is not null)
        {
            UpdateDrawPreview(cell);
        }
    }

    public void Released()
    {
        _drag = null;
        if (_drawStart is null) return;

        _drawStart = null;
        IsDrawing = false;
        CreateArea(DrawPreview);
    }

    public string CursorText(Point world)
    {
        var cell = Cell(world);
        return $"Cell {(int) (cell.X / GridSize)}, {(int) (cell.Y / GridSize)}";
    }

    #endregion

    #region Editing

    public bool Delete()
    {
        if (editor.SelectedMap is not { } map || editor.SelectedArea is not { } area) return false;

        map.Areas.Remove(area.Area);
        editor.Areas.Remove(area);
        editor.Select(null);
        return true;
    }

    public bool Copy() => false;

    public bool Paste(Point world) => false;

    // one cell at a time (big: a whole tile)
    public void Nudge(int dx, int dy, bool big)
    {
        if (editor.SelectedArea is not { } area) return;

        var step = big ? Math.Max(GridSize, editor.TileSize) : GridSize;
        var x = Math.Clamp(area.X + dx * step, 0, Math.Max(0, editor.MapPixelWidth - area.Width));
        var y = Math.Clamp(area.Y + dy * step, 0, Math.Max(0, editor.MapPixelHeight - area.Height));
        area.MoveTo((int) x, (int) y);
    }

    private void CreateArea(Rect bounds)
    {
        if (editor.SelectedMap is not { } map || bounds.Width <= 0 || bounds.Height <= 0) return;

        var id = Calculations.GetNextId([.. map.Areas.Select(x => x.Id)]);
        var area = new MapArea
        {
            Id = id,
            Name = NewAreaType == MapAreaType.Area ? $"Area {id:000}" : string.Empty,
            AreaType = NewAreaType,
            OffsetX = (int) bounds.X,
            OffsetY = (int) bounds.Y,
            Width = (int) bounds.Width,
            Height = (int) bounds.Height
        };

        map.Areas.Add(area);
        var model = new MapAreaModel(area, GridSize);
        editor.Areas.Add(model);
        editor.Select(model);
    }

    #endregion

    #region Private Methods

    // inclusive of both cells so a single click draws a one cell area
    private void UpdateDrawPreview(Point cell)
    {
        if (_drawStart is not { } start) return;

        var left = Math.Min(start.X, cell.X);
        var top = Math.Min(start.Y, cell.Y);
        var right = Math.Max(start.X, cell.X) + GridSize;
        var bottom = Math.Max(start.Y, cell.Y) + GridSize;
        DrawPreview = new Rect(left, top, right - left, bottom - top);
        IsDrawing = true;
    }

    private Point Cell(Point world) => EditorGrid.ToCell(world, GridSize, editor.MapPixelWidth, editor.MapPixelHeight);

    #endregion
}
