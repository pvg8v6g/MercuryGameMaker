using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Models.MainPage;

namespace GameMaker.UX.Utilities;

/// <summary>
/// The map editor's coordinate maths: from the screen to the map, and snapping a point to a tile, a cell of the
/// area grid or the character step grid. Everything is in unscaled map pixels and kept inside the map.
/// </summary>
public static class EditorGrid
{
    #region Methods

    public static Point ToWorld(Point screen, EditorCamera camera) => new(screen.X + camera.X, screen.Y + camera.Y);

    public static bool IsInside(Point world, double mapWidth, double mapHeight)
    {
        return world is { X: >= 0, Y: >= 0 } && world.X < mapWidth && world.Y < mapHeight;
    }

    // the top left of the cell of this size that the point is in
    public static Point ToCell(Point world, int size, double mapWidth, double mapHeight)
    {
        var x = Math.Clamp(Calculations.RoundToMultiple(world.X, size, Rounding.Down), 0, Math.Max(0, mapWidth - size));
        var y = Math.Clamp(Calculations.RoundToMultiple(world.Y, size, Rounding.Down), 0, Math.Max(0, mapHeight - size));
        return new Point(x, y);
    }

    // the nearest point on the step grid (as far as a character moves in one step), so nothing is placed between
    // two places a character can stand
    public static Point ToStep(Point world, int step, double mapWidth, double mapHeight)
    {
        var x = Math.Clamp(Math.Round(world.X / step) * step, 0, mapWidth);
        var y = Math.Clamp(Math.Round(world.Y / step) * step, 0, mapHeight);
        return new Point(x, y);
    }

    // the middle of the bottom edge of the tile the point is in, where a prop or character stands on that tile
    public static Point ToTileBase(Point world, int tileSize, double mapWidth, double mapHeight)
    {
        var tile = ToCell(world, tileSize, mapWidth, mapHeight);
        return new Point(tile.X + tileSize / 2.0, tile.Y + tileSize);
    }

    #endregion
}
