using Windows.Foundation;
using GameLibrary.Models;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;

namespace GameEngine.Interface;

/// <summary>
/// Cuts the pictures the screen layers show out of their sheets: a face from Graphics\Faces and an icon from the icon
/// sheets. Each answer is the sheet's path with the cell to show, so it can be measured off the ui thread and the image
/// made on it.
/// </summary>
public class InterfaceImages(IGraphicsService graphicsService, ILocationService locationService)
{
    #region Methods

    // the face cell on its sheet in Graphics\Faces, none when there's no face
    public async Task<(string path, Rect cell)?> FaceCell(string? faceName, int? faceIndex)
    {
        if (string.IsNullOrEmpty(faceName) || locationService.GraphicsDirectory is not { } graphics) return null;

        var path = Path.Combine(graphics, "Faces", faceName);
        if (!File.Exists(path)) return null;

        // face sheets are 4 across with any number of rows, so the cells are square (the editor's chooser cuts them
        // the same way)
        var (width, _) = await graphicsService.GetImageDimensions(path);
        var (cellWidth, cellHeight) = await graphicsService.GetSegmentation(path);
        var columns = Math.Max(1, (int) Math.Round(width / cellWidth));
        var index = faceIndex ?? 0;
        return (path, new Rect((index % columns) * cellWidth, (index / columns) * cellHeight, cellWidth, cellHeight));
    }

    // an icon's cell on its icon sheet, none when it has no icon
    public async Task<(string path, Rect cell)?> IconCell(GameIcon icon)
    {
        if (icon.IsNone) return null;

        var path = graphicsService.GetIconSheetPath(icon.Sheet);
        if (!File.Exists(path)) return null;

        var size = graphicsService.IconSize;
        var (width, _) = await graphicsService.GetImageDimensions(path);
        var columns = Math.Max(1, (int) (width / size));
        return (path, new Rect((icon.Index % columns) * size, (icon.Index / columns) * size, size, size));
    }

    #endregion
}
