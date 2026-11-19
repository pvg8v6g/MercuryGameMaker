using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Models.MainPage;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.MainPage.Tools;

/// <summary>
/// Paths mode: patrol paths on the map. Click a tile to add a point to the picked path (after the picked point), click a
/// point to pick it, drag it to move it. Delete removes the picked point. Points sit in the middle of a tile.
/// </summary>
public partial class PathTool(MainPageViewModel editor) : PropertyChangedUpdater, IMapTool
{
    #region Fields

    private PatrolPoint? _drag;

    #endregion

    #region Properties

    public PatrolStyle[] Styles { get; } = Enum.GetValues<PatrolStyle>();

    // the selected map's paths
    public ObservableCollection<PatrolPath> Paths { get; } = [];

    public PatrolPath? SelectedPath
    {
        get;
        set
        {
            if (field == value) return;
            field?.PropertyChanged -= PathChanged;
            SetField(ref field, value);
            field?.PropertyChanged += PathChanged;
            SelectedPoint = value?.Points.LastOrDefault();
            OnPropertyChanged(nameof(HasSelectedPath));
            Redraw();
        }
    }

    public PatrolPoint? SelectedPoint
    {
        get;
        set
        {
            if (field == value) return;
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasSelectedPoint));
            OnPropertyChanged(nameof(SelectedPointPause));
            OnPropertyChanged(nameof(SelectedPointTitle));
            Redraw();
        }
    }

    // the number box works in doubles, the point keeps whole milliseconds
    public double SelectedPointPause
    {
        get => SelectedPoint?.PauseTime ?? 0;
        set
        {
            if (SelectedPoint is not { } point || double.IsNaN(value)) return;
            point.PauseTime = Math.Max(0, (int) value);
            OnPropertyChanged();
        }
    }

    public string SelectedPointTitle => SelectedPath is { } path && SelectedPoint is { } point
        ? $"Point {path.Points.IndexOf(point) + 1} (tile {point.X}, {point.Y})"
        : string.Empty;

    public bool HasSelectedPath => SelectedPath is not null;

    public bool HasSelectedPoint => SelectedPoint is not null;

    // what the map draws: a line between points in walking order and a numbered dot on each point
    public ObservableCollection<PathSegment> Segments { get; } = [];

    public ObservableCollection<PathMarker> Markers { get; } = [];

    public int SnapSize => editor.TileSize;

    public bool CanPaste => false;

    #endregion

    #region Commands

    public RelayCommand NewPathCommand => field ??= new RelayCommand(() => NewPath());

    public RelayCommand DeletePathCommand => field ??= new RelayCommand(DeletePath);

    #endregion

    #region Methods

    // the map's paths, the first one picked
    public void Load(GameMap? map)
    {
        Paths.Clear();
        if (map is not null)
        {
            foreach (var path in map.PatrolPaths) Paths.Add(path);
        }

        SelectedPath = Paths.FirstOrDefault();
        Redraw();
    }

    #endregion

    #region Input

    public void Pressed(Point world, bool leftButton)
    {
        var (x, y) = Tile(world);
        var hit = PointAt(x, y);
        if (hit is { } found)
        {
            SelectedPath = found.Path;
            SelectedPoint = found.Point;
            if (leftButton) _drag = found.Point;
            return;
        }

        if (!leftButton) return;

        var path = SelectedPath ?? NewPath();
        if (path is null) return;

        var point = new PatrolPoint { X = x, Y = y };
        var index = SelectedPoint is { } after ? path.Points.IndexOf(after) + 1 : path.Points.Count;
        path.Points.Insert(index, point);
        SelectedPoint = point;
        Redraw();
    }

    public void Moved(Point world, bool inside)
    {
        if (_drag is null) return;

        var (x, y) = Tile(world);
        if (_drag.X == x && _drag.Y == y) return;

        _drag.X = x;
        _drag.Y = y;
        Redraw();
    }

    public void Released() => _drag = null;

    // a press picks what's under the cursor first, so a double-click edits the picked one
    public async Task DoubleTapped(Point world)
    {
        await editor.EditSelection();
    }

    public string CursorText(Point world)
    {
        var (x, y) = Tile(world);
        return $"Tile {x}, {y}";
    }

    #endregion

    #region Editing

    // removes the picked point, the one before it is picked next
    public bool Delete()
    {
        if (SelectedPath is not { } path || SelectedPoint is not { } point) return false;

        var index = path.Points.IndexOf(point);
        path.Points.Remove(point);
        SelectedPoint = path.Points.Count == 0 ? null : path.Points[Math.Max(0, index - 1)];
        Redraw();
        return true;
    }

    public bool Copy() => false;

    public bool Paste(Point world) => false;

    // one tile at a time either way
    public void Nudge(int dx, int dy, bool big)
    {
        if (SelectedPoint is not { } point) return;

        point.X = Math.Clamp(point.X + dx, 0, Math.Max(0, (int) (editor.MapPixelWidth / editor.TileSize) - 1));
        point.Y = Math.Clamp(point.Y + dy, 0, Math.Max(0, (int) (editor.MapPixelHeight / editor.TileSize) - 1));
        Redraw();
    }

    #endregion

    #region Private Methods

    private PatrolPath? NewPath()
    {
        if (editor.SelectedMap is not { } map) return null;

        var id = Calculations.GetNextId([.. map.PatrolPaths.Select(x => x.Id)]);
        var path = new PatrolPath { Id = id, Name = $"Path {id:000}" };
        map.PatrolPaths.Add(path);
        Paths.Add(path);
        SelectedPath = path;
        return path;
    }

    private void DeletePath()
    {
        if (editor.SelectedMap is not { } map || SelectedPath is not { } path) return;

        map.PatrolPaths.Remove(path);
        Paths.Remove(path);
        SelectedPath = Paths.FirstOrDefault();
        Redraw();
    }

    // a renamed path shows its new name, a new style can close or open the loop
    private void PathChanged(object? sender, PropertyChangedEventArgs e) => Redraw();

    // the point on a tile, the picked path's first
    private (PatrolPath Path, PatrolPoint Point)? PointAt(int x, int y)
    {
        foreach (var path in Paths.OrderByDescending(path => path == SelectedPath))
        {
            if (path.Points.FirstOrDefault(point => point.X == x && point.Y == y) is { } point) return (path, point);
        }

        return null;
    }

    private (int X, int Y) Tile(Point world)
    {
        var tile = editor.TileSize;
        var x = Math.Clamp((int) (world.X / tile), 0, Math.Max(0, (int) (editor.MapPixelWidth / tile) - 1));
        var y = Math.Clamp((int) (world.Y / tile), 0, Math.Max(0, (int) (editor.MapPixelHeight / tile) - 1));
        return (x, y);
    }

    // every path's lines and dots, the picked path on top and in orange while the paths tool is in use
    public void Refresh() => Redraw();

    private void Redraw()
    {
        Segments.Clear();
        Markers.Clear();
        var tile = editor.TileSize;
        foreach (var path in Paths.OrderBy(path => path == SelectedPath))
        {
            var picked = path == SelectedPath && editor.IsPathMode;
            var centers = path.Points.Select(point => (X: (point.X + 0.5) * tile, Y: (point.Y + 0.5) * tile)).ToArray();

            // wandering goes from any point to any other so it has no lines, a loop walks from the last point back to the first
            if (path.Style != PatrolStyle.Wander)
            {
                for (var i = 0; i + 1 < centers.Length; i++) Segments.Add(new PathSegment(centers[i].X, centers[i].Y, centers[i + 1].X, centers[i + 1].Y, picked));
                if (path.Style == PatrolStyle.Loop && centers.Length > 2) Segments.Add(new PathSegment(centers[^1].X, centers[^1].Y, centers[0].X, centers[0].Y, picked));
            }

            // the first dot carries the path's name
            for (var i = 0; i < centers.Length; i++)
            {
                Markers.Add(new PathMarker(centers[i].X, centers[i].Y, $"{i + 1}", picked, picked && path.Points[i] == SelectedPoint, i == 0 ? path.Name : null));
            }
        }
    }

    #endregion
}
