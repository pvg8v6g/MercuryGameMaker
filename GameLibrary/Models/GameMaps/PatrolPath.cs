using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// A named line of points on a map that entities patrol. Many entities can share one path, each starting at its own
/// point. The entities find their own way between points, so the points only mark the turns and the stops.
/// </summary>
public partial class PatrolPath : BaseModel
{
    public PatrolStyle Style
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<PatrolPoint> Points { get; init; } = [];
}

/// <summary>
/// A stop on a patrol path, the middle of a tile. A patroller can wait there and look around before going on.
/// </summary>
public partial class PatrolPoint : PropertyChangedUpdater
{
    // the tile, counted in tiles from the top left of the map
    public int X
    {
        get;
        set => SetField(ref field, value);
    }

    public int Y
    {
        get;
        set => SetField(ref field, value);
    }

    // how long to stand here in milliseconds, the look directions are spread over it
    public int PauseTime
    {
        get;
        set => SetField(ref field, value);
    }

    // the ways to look while standing here, in this order
    public bool LookUp
    {
        get;
        set => SetField(ref field, value);
    }

    public bool LookRight
    {
        get;
        set => SetField(ref field, value);
    }

    public bool LookDown
    {
        get;
        set => SetField(ref field, value);
    }

    public bool LookLeft
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction[] Looks()
    {
        var looks = new List<Direction>();
        if (LookUp) looks.Add(Direction.Up);
        if (LookRight) looks.Add(Direction.Right);
        if (LookDown) looks.Add(Direction.Down);
        if (LookLeft) looks.Add(Direction.Left);
        return [.. looks];
    }
}
