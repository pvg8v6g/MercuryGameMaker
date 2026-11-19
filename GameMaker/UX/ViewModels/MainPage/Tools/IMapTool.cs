using Windows.Foundation;

namespace GameMaker.UX.ViewModels.MainPage.Tools;

/// <summary>
/// What the map does with the mouse and the keyboard in one edit mode (entities, areas, props).
/// The map editor sends every press, move and shortcut to the tool of the mode it is in.
/// Points are in unscaled map pixels.
/// </summary>
public interface IMapTool
{
    // what the cursor snaps to, the hover box and the grid are drawn at this size
    int SnapSize { get; }

    bool CanPaste { get; }

    void Pressed(Point world, bool leftButton);

    // inside: the cursor is over the map
    void Moved(Point world, bool inside);

    void Released();

    // opens the editor of what was double-clicked (a new entity on an empty tile)
    Task DoubleTapped(Point world);

    // shown next to the toolbar while the cursor is over the map
    string CursorText(Point world);

    // these return false when the tool has nothing selected to act on
    bool Delete();

    bool Copy();

    bool Paste(Point world);

    // one step in a direction, big: a whole tile
    void Nudge(int dx, int dy, bool big);
}
