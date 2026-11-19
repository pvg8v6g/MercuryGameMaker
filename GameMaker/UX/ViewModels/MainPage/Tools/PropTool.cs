using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Models.GameMaps;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using GameLibrary.Utilities;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.ViewModels.MainPage.Tools;

/// <summary>
/// Props mode: pick a prop in the palette and click the map to stamp it, click a prop to select it, drag to move it.
/// A prop's position is its base point (the bottom middle of its picture, where it meets the ground) and it sits on
/// the character step grid, so it is never between two places a character can stand.
/// </summary>
public partial class PropTool(
    MainPageViewModel editor,
    IGameDataService gameDataService,
    IGraphicsService graphicsService,
    ILocationService locationService) : PropertyChangedUpdater, IMapTool
{
    #region Fields

    private MapPropModel? _drag;
    private Point _dragOffset;
    private MapProp? _copied;
    private bool _hovering;

    // picture sizes, read once per file
    private readonly Dictionary<string, Size> _sizes = [];

    #endregion

    #region Properties

    // the props placed on the selected map
    public ObservableCollection<MapPropModel> Props { get; } = [];

    // every placed prop's footprint in map pixels, drawn with the areas
    public ObservableCollection<Rect> Footprints { get; } = [];

    // the palette: every catalogue prop that has a picture
    public ObservableCollection<PropChoiceModel> Choices { get; } = [];

    public PropChoiceModel? SelectedChoice
    {
        get;
        set
        {
            SetField(ref field, value);
            GhostImage = value?.Image;
            OnPropertyChanged(nameof(IsGhostShown));
        }
    }

    // stamped and moved props put their base on the middle of the bottom of a tile
    public bool SnapToTile
    {
        get;
        set
        {
            SetField(ref field, value);
            editor.OnSnapSizeChanged();
        }
    }

    public int SnapSize => SnapToTile ? editor.TileSize : gameDataService.StepSize;

    public bool CanPaste => _copied is not null;

    // the picked prop following the cursor, where a click would stamp it
    public BitmapImage? GhostImage
    {
        get;
        private set => SetField(ref field, value);
    }

    public double GhostX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double GhostY
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool IsGhostShown => editor.IsPropMode && _hovering && GhostImage is not null && _drag is null;

    public AsyncRelayCommand RefreshChoicesCommand => field ??= new AsyncRelayCommand(LoadChoices);

    #endregion

    #region Loading

    public async Task LoadChoices()
    {
        var keep = SelectedChoice?.Prop.Guid;
        var choices = new List<PropChoiceModel>();
        foreach (var prop in gameDataService.Props.OrderBy(x => x.Id))
        {
            if (ImagePath(prop.ImageName) is not { } path) continue;

            var size = await Size(path);
            var drawn = await graphicsService.GetPropImagePath(path, false, prop.BuryDepth, prop.BuryFeather);
            choices.Add(new PropChoiceModel(prop, path, await graphicsService.GetImage(drawn), size.Width, size.Height));
        }

        Choices.Clear();
        foreach (var choice in choices) Choices.Add(choice);
        SelectedChoice = Choices.FirstOrDefault(x => x.Prop.Guid == keep) ?? Choices.FirstOrDefault();
    }

    // the props of the selected map, a prop whose catalogue entry or picture is gone is skipped (and kept in the map)
    public async Task LoadMapProps()
    {
        var map = editor.SelectedMap;
        var models = new List<MapPropModel>();
        if (editor.IsMapSelected && map is not null)
        {
            foreach (var placed in map.Props)
            {
                if (await CreateModel(placed) is { } model) models.Add(model);
            }
        }

        if (!Equals(map, editor.SelectedMap)) return;
        Props.Clear();
        foreach (var model in models) Props.Add(model);
    }

    // called after the scene is rebuilt, the footprints follow the props
    public void RefreshFootprints()
    {
        Footprints.Clear();
        foreach (var box in Props.SelectMany(x => x.Footprint)) Footprints.Add(box);
    }

    public void OnModeChanged() => OnPropertyChanged(nameof(IsGhostShown));

    #endregion

    #region Input

    public void Pressed(Point world, bool leftButton)
    {
        // the prop drawn in front is the one picked when props overlap
        var prop = Props.Where(x => x.Contains(world.X, world.Y)).MaxBy(x => x.SortBottom);
        if (prop is not null)
        {
            editor.Select(prop);
            if (!leftButton) return;

            _drag = prop;
            _dragOffset = new Point(world.X - prop.Placed.X, world.Y - prop.Placed.Y);
            OnPropertyChanged(nameof(IsGhostShown));
            return;
        }

        editor.Select(null);
        if (leftButton) Stamp(world);
    }

    public void Moved(Point world, bool inside)
    {
        if (_hovering != inside)
        {
            _hovering = inside;
            OnPropertyChanged(nameof(IsGhostShown));
        }

        var basePoint = Base(world);
        if (SelectedChoice is { } choice)
        {
            GhostX = basePoint.X - choice.Width / 2.0;
            GhostY = basePoint.Y - choice.Height;
        }

        if (_drag is null) return;

        var target = Base(new Point(world.X - _dragOffset.X, world.Y - _dragOffset.Y));
        if ((int) target.X == _drag.Placed.X && (int) target.Y == _drag.Placed.Y) return;

        _drag.MoveTo((int) target.X, (int) target.Y);
        if (_drag.Picture is { } picture) editor.Scene.Move(picture, _drag.X, _drag.Y, editor.Camera);
    }

    // re-sorts once the move is done so the prop draws in front of/behind its new neighbours
    public void Released()
    {
        if (_drag is null) return;
        _drag = null;
        OnPropertyChanged(nameof(IsGhostShown));
        editor.RebuildScene();
    }

    // a press picks what's under the cursor first, so a double-click edits the picked one
    public async Task DoubleTapped(Point world)
    {
        await editor.EditSelection();
    }

    public string CursorText(Point world)
    {
        var basePoint = Base(world);
        return $"{(int) basePoint.X}, {(int) basePoint.Y} px";
    }

    #endregion

    #region Editing

    public bool Delete()
    {
        if (editor.SelectedMap is not { } map || editor.SelectedProp is not { } prop) return false;

        map.Props.Remove(prop.Placed);
        Props.Remove(prop);
        editor.Select(null);
        editor.RebuildScene();
        return true;
    }

    public bool Copy()
    {
        if (editor.SelectedProp is not { } prop) return false;

        _copied = new MapProp { PropGuid = prop.Placed.PropGuid, Flip = prop.Placed.Flip };
        return true;
    }

    // a copied prop is pasted with its base where the cursor is
    public bool Paste(Point world)
    {
        if (editor.SelectedMap is not { } map || _copied is null) return false;

        var basePoint = Base(world);
        var placed = new MapProp { PropGuid = _copied.PropGuid, Flip = _copied.Flip, X = (int) basePoint.X, Y = (int) basePoint.Y };
        map.Props.Add(placed);
        Add(placed, select: true).Forget();
        return true;
    }

    // one character step at a time (big: a whole tile)
    public void Nudge(int dx, int dy, bool big)
    {
        if (editor.SelectedProp is not { } prop) return;

        var step = big ? editor.TileSize : gameDataService.StepSize;
        prop.MoveTo(Math.Clamp(prop.Placed.X + dx * step, 0, editor.MapPixelWidth), Math.Clamp(prop.Placed.Y + dy * step, 0, editor.MapPixelHeight));
        editor.RebuildScene();
    }

    public void FlipSelected()
    {
        if (editor.SelectedProp is { } model) Flip(model).Forget();
    }

    // places props from a props file (written by the map composer): [{"image": "tree_oak.png", "x": 560, "y": 360,
    // "flip": false}, ...] where x/y is the base point. Pictures are matched to catalogue props by their picture,
    // the result says how many were placed and which pictures have no prop yet
    public async Task<string> Import(string path)
    {
        if (editor.SelectedMap is not { } map) return "Pick a map first.";

        List<PropPlacement>? placements;
        try
        {
            placements = System.Text.Json.JsonSerializer.Deserialize<List<PropPlacement>>(await File.ReadAllTextAsync(path),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception exception)
        {
            return $"That file couldn't be read as a props file: {exception.Message}";
        }

        var placed = 0;
        var missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var placement in placements ?? [])
        {
            var prop = gameDataService.Props.FirstOrDefault(x => string.Equals(x.ImageName, placement.Image, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
            {
                missing.Add(placement.Image ?? "(no picture)");
                continue;
            }

            var mapProp = new MapProp
            {
                PropGuid = prop.Guid,
                X = Math.Clamp(placement.X, 0, editor.MapPixelWidth),
                Y = Math.Clamp(placement.Y, 0, editor.MapPixelHeight),
                Flip = placement.Flip
            };
            map.Props.Add(mapProp);
            if (await CreateModel(mapProp) is { } model) Props.Add(model);
            placed++;
        }

        editor.RebuildScene();
        var result = $"Placed {placed} prop{(placed == 1 ? "" : "s")} on {map.Name}.";
        if (missing.Count > 0) result += $"\n\nSkipped pictures with no prop on the Props page yet: {string.Join(", ", missing)}.";
        return result;
    }

    // puts new props on the selected map (from a fill), drawn once at the end
    public async Task AddMany(IEnumerable<MapProp> placed)
    {
        if (editor.SelectedMap is not { } map) return;

        foreach (var mapProp in placed)
        {
            map.Props.Add(mapProp);
            if (await CreateModel(mapProp) is { } model) Props.Add(model);
        }

        editor.RebuildScene();
    }

    public void ClearAll()
    {
        if (editor.SelectedMap is not { } map) return;

        map.Props.Clear();
        Props.Clear();
        editor.Select(null);
        editor.RebuildScene();
    }

    private sealed record PropPlacement(string? Image, int X, int Y, bool Flip);

    private void Stamp(Point world)
    {
        if (editor.SelectedMap is not { } map || SelectedChoice is not { } choice) return;

        var basePoint = Base(world);
        var placed = new MapProp { PropGuid = choice.Prop.Guid, X = (int) basePoint.X, Y = (int) basePoint.Y };
        map.Props.Add(placed);
        Add(placed, select: false).Forget();
    }

    private async Task Add(MapProp placed, bool select)
    {
        if (await CreateModel(placed) is not { } model) return;

        Props.Add(model);
        if (select) editor.Select(model);
        editor.RebuildScene();
    }

    private async Task Flip(MapPropModel model)
    {
        model.Placed.Flip = !model.Placed.Flip;
        if (ImagePath(model.Prop.ImageName) is { } path) model.ImagePath = await graphicsService.GetPropImagePath(path, model.Placed.Flip, model.Prop.BuryDepth, model.Prop.BuryFeather);
        model.Refresh();
        editor.RebuildScene();
    }

    #endregion

    #region Private Methods

    // where a prop's base goes for the cursor: on the step grid, or the bottom middle of its tile when snapping to tiles
    private Point Base(Point world)
    {
        return SnapToTile
            ? EditorGrid.ToTileBase(world, editor.TileSize, editor.MapPixelWidth, editor.MapPixelHeight)
            : EditorGrid.ToStep(world, gameDataService.StepSize, editor.MapPixelWidth, editor.MapPixelHeight);
    }

    private async Task<MapPropModel?> CreateModel(MapProp placed)
    {
        if (gameDataService.Props.FirstOrDefault(x => x.Guid == placed.PropGuid) is not { } prop) return null;
        if (ImagePath(prop.ImageName) is not { } path) return null;

        var size = await Size(path);
        var drawn = await graphicsService.GetPropImagePath(path, placed.Flip, prop.BuryDepth, prop.BuryFeather);
        var model = new MapPropModel(placed, prop, drawn, size.Width, size.Height);
        model.Refresh();
        return model;
    }

    private string? ImagePath(string? imageName)
    {
        if (string.IsNullOrEmpty(imageName)) return null;
        var path = Path.Combine(locationService.GraphicsDirectory!, "Props", imageName);
        return File.Exists(path) ? path : null;
    }

    private async Task<Size> Size(string path)
    {
        if (_sizes.TryGetValue(path, out var size)) return size;

        var (width, height) = await graphicsService.GetImageDimensions(path);
        return _sizes[path] = new Size(width, height);
    }

    #endregion
}
